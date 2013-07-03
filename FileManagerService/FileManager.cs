using System;
using System.Collections.Generic;
using System.Data;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;

using FileManagerService.Models;

namespace FileManagerService
{
    public class FileManager
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
            return GetFolder(path, ReadType.Default);
        }

        public Folder GetFolder(string path, ReadType readType)
        {
            if (!ValidatePath(path)) return null;
            SqlExpressionVisitor<Folder> folderEV = OrmLiteConfig.DialectProvider.ExpressionVisitor<Folder>();
            folderEV.Where(f => f.FullPath == path);

            folderEV.Where(f => f.IsPurged == false);
            if (readType == ReadType.Default) folderEV.Where(f => f.IsDeleted == false);
            if (readType == ReadType.OnlyDeleted) folderEV.Where(f => f.IsDeleted == true);

            return db.FirstOrDefault<Folder>(folderEV);
        }

        public Folder GetFolder(long folderId)
        {
            return db.GetById<Folder>(folderId);
        }

        public File GetFile(Folder parentFolder, string fileName)
        {
            if (parentFolder == null) throw new ArgumentNullException("parentFolder");
            if (string.IsNullOrWhiteSpace(fileName) == true) throw new ArgumentNullException("fileName");

            string sql = @"
SELECT f.FileId as Id, f.Name, f.ParentFolderId, f.IsDeleted, f.IsPurged, f.CreatedTimeStamp, f.LastModifiedTimestamp
	,fv.FileStore, fv.Version as CurrentVersion, fv.Size, d.FullPath + f.Name as FullPath
FROM [File] f
INNER JOIN [FileVersion] fv
	ON	fv.FileId = f.FileId
	AND	fv.IsCurrent = 1
	AND fv.IsDeleted = 0
INNER JOIN [Folder] d
	ON	d.FolderId = f.ParentFolderId
WHERE d.FolderId = {0} 
  AND f.IsDeleted = 0
  AND f.Name = {1}";


            List<File> files = db.Query<File>(sql.Params(parentFolder.Id, fileName));
            if (files.Count > 1) throw new InvalidOperationException("Multiple files matched in this folder");
            return files.FirstOrDefault();
        }

        public File GetFile(long fileId)
        {
            string sql = @"
SELECT f.FileId as Id, f.Name, f.ParentFolderId, f.IsDeleted, f.IsPurged, f.CreatedTimeStamp, f.LastModifiedTimestamp
	,fv.FileStore, fv.Version as CurrentVersion, fv.Size, d.FullPath + f.Name as FullPath
FROM [File] f
INNER JOIN [FileVersion] fv
	ON	fv.FileId = f.FileId
	AND	fv.IsCurrent = 1
	AND fv.IsDeleted = 0
INNER JOIN [Folder] d
	ON	d.FolderId = f.ParentFolderId
WHERE f.IsDeleted = 0
  AND f.FileId = {0}";

            List<File> files = db.Query<File>(sql.Params(fileId));
            if (files.Count > 1) throw new InvalidOperationException("Multiple files matched.");
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
            return GetChildren(folder, ReadType.Default, out folders, out files);
        }

        public Result GetChildren(Folder folder, ReadType readType, out IEnumerable<Folder> folders, out IEnumerable<File> files)
        {
            if (folder == null) throw new ArgumentNullException("folder");
            folders = new List<Folder>();
            files = new List<File>();

            SqlExpressionVisitor<Folder> folderEV = OrmLiteConfig.DialectProvider.ExpressionVisitor<Folder>();
            folderEV.Where(f => f.ParentFolderId == folder.Id);
            if (readType == ReadType.Default) folderEV.Where(f => f.IsDeleted == false);
            if (readType == ReadType.OnlyDeleted) folderEV.Where(f => f.IsDeleted == true);
            folders = db.Select<Folder>(folderEV);

            var sql = new StringBuilder(@"
SELECT f.FileId as Id, f.Name, f.ParentFolderId, f.IsDeleted, f.IsPurged, f.CreatedTimeStamp, f.LastModifiedTimestamp
	,fv.FileStore, fv.Version as CurrentVersion, fv.Size, d.FullPath + f.Name as FullPath
FROM [File] f
INNER JOIN [FileVersion] fv
	ON	fv.FileId = f.FileId
	AND	fv.IsCurrent = 1
INNER JOIN [Folder] d
    ON  d.FolderId = f.ParentFolderId
WHERE f.ParentFolderId = {0}".Params(folder.Id));

            if (readType.HasFlag(ReadType.OnlyDeleted)) sql.Append(" AND f.IsDeleted = 1");
            if (readType.HasFlag(ReadType.OnlyNonDeleted)) sql.Append(" AND f.IsDeleted = 0");
            files = db.Query<File>(sql.ToString());

            return Result.Success;
        }

        public Result CreateFolder(Folder parentFolder, string name, out Folder folder)
        {
            return CreateFolder(parentFolder, name, OverwriteBehavior.RaiseConflict, out folder);
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
                if (behavior == OverwriteBehavior.RaiseConflict)
                {
                    folder = null;
                    return Result.FolderAlreadyExists;
                }
                // Behavior is overwrite, we'll just return the existing folder
                //else if (behavior == OverwriteBehavior.Overwrite)
                //{
                //    return Result.Success;
                //}
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

        public Result CopyFolder(Folder source, Folder target, out Folder newFolder)
        {
            return CopyFolder(source, target, OverwriteBehavior.Copy, out newFolder);
        }

        /// <summary>
        /// Copy the contents of one folder to another.  Only tip versions of files are copied.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="behavior"></param>
        /// <returns></returns>
        public Result CopyFolder(Folder source, Folder target, OverwriteBehavior behavior, out Folder newFolder)
        {
            newFolder = null;

            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("parentTarget");
            if (source.IsDeleted || target.IsDeleted) return Result.CannotModifyDeletedItems;

            // Validate we aren't copying the source into one of it's childs
            // TODO - is a name match okay, or do we need to actually query out?
            if (target.FullPath.StartsWith(source.FullPath))
            {
                return Result.TargetIsChildOfSource;
            }

            //if (behavior == OverwriteBehavior.Overwrite) throw new ArgumentException("Behavior cannot be overwrite for copy operation.");

            // Create the base folder that we're copying from
            CreateFolder(target, source.Name, OverwriteBehavior.Copy, out newFolder);

            CopyFolderInternal(source, newFolder, behavior);
            return Result.Success;
        }

        public Result MoveFolder(Folder source, Folder target)
        {
            return MoveFolder(source, target, OverwriteBehavior.Copy);
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
            return MoveFolderInternal(source, target, behavior, false);
        }

        private Result MoveFolderInternal(Folder source, Folder target, OverwriteBehavior behavior, bool restoreFolder)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("parentTarget");
            if ((source.IsDeleted && !restoreFolder) || target.IsDeleted) return Result.CannotModifyDeletedItems;

            // Validate we aren't copying the source into one of it's childs
            if (target.FullPath.StartsWith(source.FullPath))
            {
                return Result.TargetIsChildOfSource;
            }

            // A move for a folder is just updating the parent folder reference (of the source) and
            // replacing part of the FullPath of any child folders with the new target path
            string oldPath = source.FullPath;
            string newPath = target.FullPath + source.Name + "/";
            string name = source.Name;

            // See if the target full path already exists, do some behavior-specific logic
            Folder targetFolder = GetFolder(newPath);
            if (targetFolder != null)
            {
                if (behavior == OverwriteBehavior.RaiseConflict)
                {
                    return Result.FolderAlreadyExists;
                }
                else if (behavior == OverwriteBehavior.Copy)
                {
                    do
                    {
                        name = name + " - Copy";
                        targetFolder = GetFolder(target.FullPath + name + "/");
                    } while (targetFolder != null);
                    newPath = target.FullPath + name + "/";
                }
            }

            foreach (Folder folder in GetChildrenRecursive(source, EnumerateChildTypes.Folders))
            {
                Regex re = new Regex(Regex.Escape(oldPath), RegexOptions.IgnoreCase);
                db.Update<Folder>(new { FullPath = re.Replace(folder.FullPath, newPath, 1), LastModifiedTimestamp = DateTime.UtcNow }, f => f.Id == folder.Id);
            }

            source.ParentFolderId = target.Id;
            source.FullPath = newPath;
            source.Name = name;
            source.LastModifiedTimestamp = DateTime.UtcNow;
            source.IsDeleted = (restoreFolder) ? false : source.IsDeleted;
            
            db.Update<Folder>(source);
            return Result.Success;
        }

        public Result RenameFolder(Folder source, string name)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
            if (source.IsDeleted) return Result.CannotModifyDeletedItems;

            string oldFullPath = source.FullPath;
            string newFullPath = GetParentFolderName(oldFullPath) + name + "/";

            if (GetFolder(newFullPath) != null)
                return Result.FolderAlreadyExists;

            foreach (Folder folder in GetChildrenRecursive(source, EnumerateChildTypes.Folders))
            {
                Regex re = new Regex(Regex.Escape(source.FullPath), RegexOptions.IgnoreCase);
                //TODO Do we update the LMT for child folders?
                db.Update<Folder>(new { FullPath = re.Replace(folder.FullPath, newFullPath, 1), LastModifiedTimestamp = DateTime.UtcNow }, f => f.Id == folder.Id);
            }

            source.Name = name;
            source.FullPath = newFullPath;
            source.LastModifiedTimestamp = DateTime.UtcNow;
            db.Update<Folder>(source);

            return Result.Success;
        }

        public Result DeleteFolder(Folder source)
        {
            if (source == null) throw new ArgumentNullException("source");
            source.IsDeleted = true;
            db.Update<Folder>(source);
            return Result.Success;
        }

        public Result RestoreFolder(Folder source)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (!source.IsDeleted) return Result.FolderIsNotDeleted;

            Folder targetFolder = GetFolder(source.FullPath);
            if (targetFolder == null)
            {
                source.IsDeleted = false;
                source.LastModifiedTimestamp = DateTime.UtcNow;
                db.Update<Folder>(source);
                return Result.Success;
            }
            else
            {
                // Target exists (was recreated), so we'll move our deleted folder to
                // a new name with - Copy at the end.
                Folder parentFolder = GetFolder(GetParentFolderName(source.FullPath));
                MoveFolderInternal(source, parentFolder, OverwriteBehavior.Copy, true);
                //source.IsDeleted = false;
                //source.LastModifiedTimestamp = DateTime.UtcNow;
                db.Update<Folder>(source);
                return Result.Success;

            }
        }

        public Result PurgeFolder(Folder source)
        {
            throw new NotImplementedException();
        }

        public Result ReadFile(File file, out IO.Stream stream)
        {
            stream = null;
            if (file == null) throw new ArgumentNullException("file");
            if (!IO.File.Exists(rootFolder + file.FileStore)) return Result.FileStoreNotFound;

            //Folder parentFolder = GetFolder(file.ParentFolderId);
            stream = new IO.FileStream(rootFolder + file.FileStore, IO.FileMode.Open);
            return Result.Success;
        }

        public Result CreateFile(Folder folder, string fileName, IO.Stream stream, out File file)
        {
            file = null;
            Guid fileStoreGuid;
            long fileSize;

            PersistStreamInternal(stream, out fileStoreGuid, out fileSize);

            File existingFile = GetFile(folder, fileName);
            long fileId;
            int nextVersion;

            if (existingFile != null)
            {
                fileId = existingFile.Id;
                nextVersion = GetNextVersion(existingFile);
            }
            else
            {
                nextVersion = 1;
                FileBase newFile = new FileBase()
                {
                    Name = fileName,
                    ParentFolderId = folder.Id,
                    FullPath = folder.FullPath + fileName,
                    CreatedTimeStamp = DateTime.UtcNow,
                    LastModifiedTimestamp = DateTime.UtcNow,
                    IsDeleted = false,
                    IsPurged = false
                };
                db.Insert<FileBase>(newFile);
                fileId = db.GetLastInsertId();
            }

            db.Update<FileVersion>("IsCurrent = 0", "FileId = {0}".Params(fileId));
            db.Insert<FileVersion>(new FileVersion() { FileId = fileId, Version = nextVersion, Size = fileSize, FileStore = fileStoreGuid, IsCurrent = true, IsDeleted = false });

            file = GetFile(fileId);
            return Result.Success;
        }

        /// <summary>
        /// Perform a version-shallow copy of the source file.  The current tip version of the <paramref name="source">source</paramref> file is the tip version
        /// off the new file, and no version history is copied.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public Result CopyFile(File source, Folder target, out File file)
        {
            return CopyFile(source, target, OverwriteBehavior.Copy, out file);
        }

        public Result CopyFile(File source, Folder target, OverwriteBehavior behavior, out File file)
        {
            return CopyFile(source, target, source.Name, behavior, out file);
        }

        public Result CopyFile(File source, Folder target, string newFileName, OverwriteBehavior behavior, out File file)
        {
            file = null;
            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("target");
            if (string.IsNullOrWhiteSpace(newFileName)) throw new ArgumentNullException("newFileName");
            if (source.IsDeleted || target.IsDeleted) return Result.CannotModifyDeletedItems;

            //See if the file already exists
            file = GetFile(target, newFileName);
            if (file != null)
            {
                if (behavior == OverwriteBehavior.RaiseConflict)
                    return Result.FileAlreadyExists;
                //else if (behavior == OverwriteBehavior.Overwrite)
                //{
                //    // ???
                //}
                else if (behavior == OverwriteBehavior.Copy)
                {
                    do
                    {
                        //Assume the extension is anything after the first ".".  Put the " - Copy" before the file extension.
                        int extensionDot = newFileName.IndexOf('.');
                        newFileName = newFileName.Substring(0, extensionDot) + " - Copy" + newFileName.Substring(extensionDot);
                        file = GetFile(target, newFileName);
                    } while (file != null);
                }
            }

            FileBase newFile = new FileBase()
            {
                Name = newFileName,
                ParentFolderId = target.Id,
                CreatedTimeStamp = DateTime.UtcNow,
                LastModifiedTimestamp = DateTime.UtcNow,
                IsDeleted = false,
                IsPurged = false
            };
            db.Insert<FileBase>(newFile);
            long fileId = db.GetLastInsertId();

            // Point to the tip version of the source file
            db.Insert<FileVersion>(new FileVersion() { FileId = fileId, Version = 1, Size = source.Size, FileStore = source.FileStore, IsCurrent = true, IsDeleted = false });

            file = GetFile(fileId);
            return Result.Success;
        }

        public Result MoveFile(File source, Folder target)
        {
            return MoveFile(source, target, OverwriteBehavior.RaiseConflict);
        }

        public Result MoveFile(File source, Folder target, OverwriteBehavior behavior)
        {
            return MoveFile(source, target, source.Name, behavior);
        }

        public Result MoveFile(File source, Folder target, string newFileName, OverwriteBehavior behavior)
        {
            return MoveFileInternal(source, target, newFileName, behavior, false);
        }

        private Result MoveFileInternal(File source, Folder target, string newFileName, OverwriteBehavior behavior, bool restoreFile)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("target");
            if (string.IsNullOrWhiteSpace(newFileName)) throw new ArgumentNullException("newFileName");
            if ((source.IsDeleted && !restoreFile) || target.IsDeleted) return Result.CannotModifyDeletedItems;

            // See if a file of the same name exists at the target
            File file = GetFile(target, newFileName);
            if (file != null)
            {
                if (behavior == OverwriteBehavior.RaiseConflict)
                    return Result.FileAlreadyExists;
                //else if (behavior == OverwriteBehavior.Overwrite)
                //{
                //    // ???
                //}
                else if (behavior == OverwriteBehavior.Copy)
                {
                    do
                    {
                        //Assume the extension is anything after the first ".".  Put the " - Copy" before the file extension.
                        int extensionDot = newFileName.IndexOf('.');
                        newFileName = newFileName.Substring(0, extensionDot) + " - Copy" + newFileName.Substring(extensionDot);
                        file = GetFile(target, newFileName);
                    } while (file != null);
                }
            }

            // Update the file's path & name
            source.Name = newFileName;
            source.LastModifiedTimestamp = DateTime.UtcNow;
            source.ParentFolderId = target.Id;

            bool isDeleted = (restoreFile) ? false : source.IsDeleted;
            db.Update<FileBase>(new { source.Name, source.LastModifiedTimestamp, source.ParentFolderId, IsDeleted = isDeleted }, fb => fb.Id == source.Id);
            return Result.Success;
        }

        public Result RenameFile(File source, string newFileName)
        {
            if (source == null) throw new ArgumentNullException("target");
            if (string.IsNullOrWhiteSpace(newFileName)) throw new ArgumentNullException("newFileName");
            if (source.IsDeleted) return Result.CannotModifyDeletedItems;

            // Get folder for file
            Folder folder = GetFolder(source.ParentFolderId.Value);
            return MoveFile(source, folder, newFileName, OverwriteBehavior.RaiseConflict);
        }

        public Result DeleteFile(File source)
        {
            if (source == null) throw new ArgumentNullException("source");
            source.IsDeleted = true;
            db.Update<FileBase>(new { IsDeleted = true }, fb => fb.Id == source.Id);
            return Result.Success;
        }

        public Result RestoreFile(File source)
        {
            if (source == null) throw new ArgumentNullException("source");
            Folder folder = GetFolder(source.ParentFolderId.Value);
            return MoveFileInternal(source, folder, source.Name, OverwriteBehavior.Copy, true);
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
            File dummy;
            foreach (File file in files)
            {
                CopyFile(file, target, out dummy);
            }

            foreach (Folder sourceFolder in folders)
            {
                Folder newTarget;
                CreateFolder(target, sourceFolder.Name, behavior, out newTarget);
                CopyFolderInternal(sourceFolder, newTarget, behavior);
            }
        }

        private enum EnumerateChildTypes
        {
            Folders = 1,
            Files = 2,
            All = 3
        }

        private IEnumerable<IHierarchyItem> GetChildrenRecursive(Folder source, EnumerateChildTypes childTypes)
        {
            List<IHierarchyItem> items = new List<IHierarchyItem>();

            IEnumerable<Folder> folders;
            IEnumerable<File> files;
            GetChildren(source, out folders, out files);

            if ((childTypes & EnumerateChildTypes.Files) == EnumerateChildTypes.Files)
            {
                items.AddRange(files);
            }

            if ((childTypes & EnumerateChildTypes.Folders) == EnumerateChildTypes.Folders)
            {
                items.AddRange(folders);
                foreach (Folder folder in folders)
                    items.AddRange(GetChildrenRecursive(folder, childTypes));
            }

            return items;
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

        /// <summary>
        /// Gets the parent folder path for the specified path.  Includes the trailing '/' so it can go right into GetFolder methods.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetParentFolderName(string path)
        {
            return path.Substring(0, path.TrimEnd('/').LastIndexOf('/') + 1);
        }

        private void PersistStreamInternal(IO.Stream stream, out Guid fileStoreGuid, out long fileSize)
        {
            fileStoreGuid = Guid.NewGuid();

            fileSize = 0;
            using (IO.FileStream fs = new IO.FileStream(rootFolder + "\\" + fileStoreGuid, IO.FileMode.Create))
            {
                byte[] buffer = new byte[1024 * 4];
                int len = -1;
                while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, len);
                    fileSize += len;
                }
            }
        }

        private int GetNextVersion(File file)
        {
            SqlExpressionVisitor<FileVersion> ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<FileVersion>();
            ev.Select(v => Sql.As(Sql.Max(v.Version), "Version")).Where(v => v.FileId == file.Id);

            var result = db.Select(ev);
            if (result.Count == 0) return 1;
            return result[0].Version + 1;
        }

    }
}