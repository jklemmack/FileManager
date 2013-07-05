using System;
using System.Collections.Generic;
using System.Data;
using IO = System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;

using FileManager;
using FileManager.Models;

namespace FileManager.Tests
{
    [TestClass]
    public class FolderTests
    {

        Manager manager;
        Folder root;

        #region Test Management

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {

        }

        [TestInitialize()]
        public void Initialize()
        {
            IDbConnection conn = GlobalInit.dbFactory.CreateDbConnection();
            conn.Open();

            manager = new Manager(conn, GlobalInit.rootFolder);
            Assert.IsNotNull(manager);

            root = manager.GetFolder("/");
        }

        [ClassCleanup()]
        public static void Cleanup()
        {

        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            //var dbConn = dbFactory.CreateDbConnection();
            //dbConn.Open();
            //dbConn.DeleteAll<FileBase>();
            //dbConn.DeleteAll<FileVersion>();
            //dbConn.DeleteAll<Folder>();


            //IO.Directory.Delete(rootFolder);
        }

        #endregion


        [TestMethod]
        [TestCategory("Folders")]
        public void Create()
        {
            Folder folder1;

            manager.CreateFolder(root, "BasicCreate", out folder1);
            Assert.IsNotNull(folder1);
            Assert.AreEqual<string>("/BasicCreate/", folder1.FullPath);
        }

        [TestMethod]
        [TestCategory("Folders")]
        public void CreateDuplicate()
        {
            Folder folder;
            Result r;
            manager.CreateFolder(root, "BasicCreateDuplicate", out folder);
            r = manager.CreateFolder(root, "BasicCreateDuplicate", out folder);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);
            Assert.IsNull(folder);

            //r = manager.CreateFolder(root, "BasicCreateDuplicate", OverwriteBehavior.Overwrite, out folder);
            //Assert.AreEqual<Result>(r, Result.Success);

            r = manager.CreateFolder(root, "BasicCreateDuplicate", OverwriteBehavior.RaiseConflict, out folder);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);

            r = manager.CreateFolder(root, "BasicCreateDuplicate", OverwriteBehavior.Copy, out folder);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(folder.Name, "BasicCreateDuplicate - Copy");

        }

        [TestMethod]
        [TestCategory("Folders")]
        public void CreateNested()
        {
            Folder folder;
            manager.CreateFolder(root, "BasicCreateNested", out folder);
            Result r = manager.CreateFolder(folder, "BasicCreateNested", out folder);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>("/BasicCreateNested/BasicCreateNested/", folder.FullPath);
        }

        [TestMethod]
        [TestCategory("Folders")]
        public void CreateNestedDuplicate()
        {
            // Create /BasicCreateNestedDuplicate / BasicCreateNested.. and dupe that final one
            Folder folder, folder2;
            Result r;
            manager.CreateFolder(root, "BasicCreateNestedDuplicate", out folder);
            manager.CreateFolder(folder, "BasicCreateNestedDuplicate", out folder2);

            r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", out folder2);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);
            Assert.IsNull(folder2);

            //r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", OverwriteBehavior.Overwrite, out folder2);
            //Assert.AreEqual<Result>(r, Result.Success);

            r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", OverwriteBehavior.RaiseConflict, out folder2);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);

            r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", OverwriteBehavior.Copy, out folder2);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(folder2.Name, "BasicCreateNestedDuplicate - Copy");
        }

        [TestMethod]
        [TestCategory("Folders")]
        public void Rename()
        {
            Folder folder1, folder2;
            Result r;

            manager.CreateFolder(root, "Rename", out folder1);
            manager.CreateFolder(folder1, "Rename", out folder2);
            r = manager.RenameFolder(folder1, "Rename1");
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(folder1.Name, "Rename1");
            folder2 = manager.GetFolder("/Rename1/Rename/");
            Assert.IsNotNull(folder2);

            manager.CreateFolder(root, "Rename", out folder2);
            r = manager.RenameFolder(folder2, "Rename1");
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);
            Assert.AreEqual<string>(folder2.Name, "Rename");
        }

        [TestMethod]
        [TestCategory("Folders")]
        public void Move()
        {
            Folder move1, move2, move3, move2b, move2c;
            Result r;

            manager.CreateFolder(root, "Move1", out move1);
            manager.CreateFolder(root, "Move2", out move2);
            manager.CreateFolder(root, "Move3", out move3);

            r = manager.MoveFolder(move3, move2);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(move3.FullPath, "/Move2/Move3/");

            r = manager.MoveFolder(move2, move1);
            Assert.IsNotNull(manager.GetFolder("/Move1/Move2/Move3/"));

            manager.CreateFolder(root, "Move2", out move2b);
            r = manager.MoveFolder(move2b, move1, OverwriteBehavior.RaiseConflict);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);

            manager.MoveFolder(move2b, move1, OverwriteBehavior.Copy);
            Assert.IsNotNull(manager.GetFolder("/Move1/Move2 - Copy/"));

            manager.CreateFolder(root, "Move2", out move2c);
            r = manager.MoveFolder(move2, root, OverwriteBehavior.RaiseConflict);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);

        }

        [TestMethod]
        [TestCategory("Folders")]
        public void Copy()
        {
            Folder copy1, copy2, copy3, newFolder;
            Result r;

            manager.CreateFolder(root, "copy1", out copy1);
            manager.CreateFolder(root, "copy2", out copy2);
            manager.CreateFolder(root, "copy3", out copy3);

            r = manager.CopyFolder(copy3, copy2, out newFolder);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.IsNotNull(manager.GetFolder("/copy2/copy3/"));

            r = manager.CopyFolder(copy3, copy2, out newFolder);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.IsNotNull(manager.GetFolder("/copy2/copy3 - Copy/"));

            Folder copy2copy3 = manager.GetFolder("/copy2/copy3/");
            //r = manager.CopyFolder(copy2copy3, copy2, out newFolder);
            r = manager.CopyFolder(copy2, copy2copy3, out newFolder);
            Assert.AreEqual<Result>(r, Result.TargetIsChildOfSource);

            r = manager.CopyFolder(copy2, copy1, out newFolder);
            Assert.AreEqual<Result>(r, Result.Success);

            IEnumerable<Folder> folders;
            IEnumerable<File> files;
            r = manager.GetChildren(copy1, out folders, out files);
            Assert.AreEqual<int>((new List<Folder>(folders)).Count, 1);
        }

        [TestMethod]
        [TestCategory("Folders")]
        public void DeleteAndRestore()
        {
            Folder toDelete, conflict;
            Result r;

            manager.CreateFolder(root, "todelete", out toDelete);
            r = manager.RestoreFolder(toDelete);
            Assert.AreEqual<Result>(r, Result.FolderIsNotDeleted);

            r = manager.DeleteFolder(toDelete);
            Assert.AreEqual<Result>(r, Result.Success);

            r = manager.RestoreFolder(toDelete);
            Assert.AreEqual<Result>(r, Result.Success);
            manager.DeleteFolder(toDelete);

            manager.CreateFolder(root, "todelete", out conflict);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(conflict.FullPath, "/todelete/");

            Folder deleted = manager.GetFolder("/todelete/", ReadType.OnlyDeleted);
            Assert.AreEqual<long>(deleted.Id, toDelete.Id);

            r = manager.RestoreFolder(deleted);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.IsFalse(deleted.IsDeleted);
            Assert.AreEqual<string>(deleted.FullPath, "/todelete - Copy/");
            Assert.AreEqual<string>(conflict.FullPath, "/todelete/");
        }

    }
}
