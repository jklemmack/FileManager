using System;
using System.Collections.Generic;
using System.Data;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;

using FileManagerService.Interfaces;
using FileManagerService.Models;

namespace FileManagerService
{
    public class FileManager : IFileManager
    {

        IDbConnection db;
        string rootFolder;

        public FileManager(IDbConnection dbConnection, string rootFolder)
        {
            if (dbConnection == null) throw new ArgumentNullException("dbConnection");
            if (rootFolder == null) throw new ArgumentNullException("rootFolder");

            if (dbConnection.State != ConnectionState.Open) throw new ArgumentException("dbConnection must be in an open state.");

            db = dbConnection;
            this.rootFolder = rootFolder;
            if (!IO.Directory.Exists(rootFolder))
                IO.Directory.CreateDirectory(rootFolder);

            //Validate the root folder exists
            Folder root = GetFolder("/");
            if (root == null)
            {
                root = new Folder()
                {
                    CreatedTimeStamp = DateTime.UtcNow,
                    FullPath = "/",
                    Name = "",
                    IsDeleted = false,
                    IsPurged = false,
                    LastModifiedTimestamp = DateTime.UtcNow,
                    ParentFolderId = null
                };
                db.Insert<Folder>(root);
            }
        }

        public Folder GetFolder(string path)
        {
            if (!ValidatePath(path)) return null;
            return db.FirstOrDefault<Folder>(f => f.FullPath == path);
        }

        public File GetFile(string path)
        {
            // Get the parent folder & the file name
            Folder parentFolder = GetFolder(GetFolderName(path));
            string fileName = GetItemName(path);

            JoinSqlBuilder<File, FileBase> join = new JoinSqlBuilder<File, FileBase>()
            .Join<FileBase, FileVersion>(f => f.Id, v => v.FileId
                , f => new { FileId = f.Id, f.Name, ParentFolder = f.ParentFolderId, f.IsDeleted, f.IsPurged, f.CreatedTimeStamp, f.LastModifiedTimestamp }
                , v => new { v.FileStore, CurrentVersion = v.Version }
                , f => f.ParentFolderId.Value == parentFolder.Id && f.Name == fileName
                , v => v.IsCurrent == true
                );

            string sql = join.ToSql();
            List<File> files = db.Query<File>(sql);
            return files.FirstOrDefault();
        }

        public IEnumerable<IHierarchyItem> GetAllItems(string path)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IHierarchyItem> GetAllChanges(DateTime sinceTimestamp)
        {
            throw new NotImplementedException();
        }

        public Result GetChildren(Folder folder, out IEnumerable<Folder> folders, out IEnumerable<File> files)
        {
            throw new NotImplementedException();
        }

        public Result GetChildren(Folder folder, ReadType readType, out IEnumerable<Folder> folders, out IEnumerable<File> files)
        {
            throw new NotImplementedException();
        }

        public Result CreateFolder(Folder parentFolder, string name, out Folder folder)
        {
            return CreateFolder(parentFolder, name, OverwriteBehavior.Skip, out folder);
        }

        public Result CreateFolder(Folder parentFolder, string name, OverwriteBehavior behavior, out Folder folder)
        {
            if (parentFolder == null) throw new ArgumentNullException("parentFolder");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            //Validate parent folder doesn't have a conflicting child
            folder = GetFolder(parentFolder.FullPath + name + "/");
            if (folder != null)
            {
                // If behavior is "skip", we toss an error
                if (behavior == OverwriteBehavior.Skip)
                {
                    folder = null;
                    return Result.FolderAlreadyExists;
                }
                // Behavior is overwrite, we'll just return the existing folder
                else if (behavior == OverwriteBehavior.Overwrite)
                {
                    return Result.Success;
                }
                // Behavior is "copy", we find a valid name with "Copy" at the end
                else if (behavior == OverwriteBehavior.Copy)
                {
                    do
                    {
                        name = name + " - Copy";
                        folder = GetFolder(parentFolder.FullPath + name + "/");
                        //if (folder == null) okay = true;
                    } while (folder != null);
                }
            }

            // Folder doesn't exist, we can create it
            folder = new Folder()
            {
                FullPath = parentFolder.FullPath + name + "/",
                Name = name,
                IsDeleted = false,
                IsPurged = false,
                CreatedTimeStamp = DateTime.UtcNow,
                LastModifiedTimestamp = DateTime.UtcNow,
                ParentFolderId = parentFolder.Id
            };


            db.Insert<Folder>(folder);
            folder.Id = db.GetLastInsertId();
            return Result.Success;
        }

        /// <summary>
        /// Copy the contents of one folder to another.  Only tip versions of files are copied.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="behavior"></param>
        /// <returns></returns>
        public Result CopyFolder(Folder source, Folder target, OverwriteBehavior behavior)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("parentTarget");

            // Validate we aren't copying the source into one of it's childs
            if (target.FullPath.StartsWith(source.FullPath))
            {
                return Result.TargetIsChildOfSource;
            }

            CopyFolderInternal(source, target, behavior);
            return Result.Success;
        }

        /// <summary>
        /// Move the contents of the source folder to the target folder.  Target folder must already exist.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="behavior"></param>
        /// <returns></returns>
        public Result MoveFolder(Folder source, Folder target, OverwriteBehavior behavior)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("parentTarget");

            // Validate we aren't copying the source into one of it's childs
            if (target.FullPath.StartsWith(source.FullPath))
            {
                return Result.TargetIsChildOfSource;
            }

            // A move for a folder is just updating the parent folder reference (of the source) and
            // replacing part of the FullPath of any child folders with the new target path
            source.ParentFolderId = target.Id;
            foreach (Folder folder in db.Select<Folder>(f => f.FullPath.StartsWith(source.FullPath)))
            {
                Regex re = new Regex(Regex.Escape(source.FullPath), RegexOptions.IgnoreCase);
                db.Update<Folder>(new { FullPath = re.Replace(folder.FullPath, target.FullPath, 1), LastModifiedTimestamp = DateTime.UtcNow }, f => f.Id == folder.Id);
            }

            return Result.Success;
        }

        public Result RenameFolder(Folder source, string name)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            string oldFullPath = source.FullPath;
            string newFullPath = GetParentFolderName(oldFullPath) + "/" + name + "/";

            if (GetFolder(newFullPath) != null)
                return Result.FolderAlreadyExists;

            foreach (Folder folder in db.Select<Folder>(f => f.FullPath.StartsWith(oldFullPath)))
            {
                Regex re = new Regex(Regex.Escape(source.FullPath), RegexOptions.IgnoreCase);
                //TODO Do we update the LMT for child folders?
                db.Update<Folder>(new { FullPath = re.Replace(folder.FullPath, newFullPath, 1), LastModifiedTimestamp = DateTime.UtcNow }, f => f.Id == folder.Id);
            }

            source.Name = name;
            source.FullPath = newFullPath;
            source.LastModifiedTimestamp = DateTime.UtcNow;

            return Result.Success;
        }

        public Result DeleteFolder(Folder source)
        {
            throw new NotImplementedException();
        }

        public Result RestoreFolder(Folder source)
        {
            throw new NotImplementedException();
        }

        public Result PurgeFolder(Folder source)
        {
            throw new NotImplementedException();
        }

        public Result ReadFile(File file, out IO.Stream stream)
        {
            throw new NotImplementedException();
        }

        public Result CreateFile(Folder folder, string name, IO.Stream stream)
        {
            throw new NotImplementedException();
        }

        public Result CopyFile(File source, string target, OverwriteBehavior behavior)
        {
            throw new NotImplementedException();
        }

        public Result MoveFile(File source, string target, OverwriteBehavior behavior)
        {
            throw new NotImplementedException();
        }

        public Result DeleteFile(File source)
        {
            throw new NotImplementedException();
        }

        public Result RestoreFile(File source)
        {
            throw new NotImplementedException();
        }

        public Result PurgeFile(File source)
        {
            throw new NotImplementedException();
        }



        private void CopyFolderInternal(Folder source, Folder target, OverwriteBehavior behavior)
        {
            IEnumerable<Folder> folders = null;
            IEnumerable<File> files = null;
            GetChildren(source, out folders, out files);
            foreach (File file in files)
            {
                CopyFileInternal(file, target, behavior);
            }

            foreach (Folder sourceFolder in folders)
            {
                Folder newTarget;
                CreateFolder(target, sourceFolder.Name, behavior, out newTarget);
                CopyFolderInternal(sourceFolder, newTarget, behavior);
            }
        }

        private void CopyFileInternal(File file, Folder target, OverwriteBehavior behavior)
        {
            throw new NotImplementedException();
        }


        private bool ValidatePath(string path)
        {
            if (path.StartsWith("/")) return true;

            return false;
        }

        private string GetFolderName(string path)
        {
            int lastSlash = path.LastIndexOf('/');
            return path.Substring(0, lastSlash);
        }

        private string GetParentFolderName(string path)
        {
            return path.Substring(0, path.TrimEnd('/').LastIndexOf('/'));
        }

        private string GetItemName(string path)
        {
            int lastSlash = path.LastIndexOf('/');
            return path.Substring(lastSlash + 1).TrimEnd('/');
        }



    }
}
