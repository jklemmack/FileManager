using System;
using System.Data;
using IO = System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;

using FileManagerService;
using FileManagerService.Models;

namespace FileManagerService.Tests
{
    [TestClass]
    public class FolderTests
    {
        const string connectionString = @"Data Source=ORB515720\SQLExpress;Initial Catalog=FileManager;Integrated Security=True";
        const string rootFolder = @"C:\Temp\Files";
        static OrmLiteConnectionFactory dbFactory;

        FileManager manager;
        Folder root;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            dbFactory = new OrmLiteConnectionFactory(connectionString, SqlServerOrmLiteDialectProvider.Instance);
            var dbConn = dbFactory.CreateDbConnection();

            dbConn.Open();
            // Drop & create the tables
            dbConn.DropAndCreateTable<FileBase>();
            dbConn.DropAndCreateTable<FileVersion>();
            dbConn.DropAndCreateTable<Folder>();

            dbConn.Close();

            if (IO.Directory.Exists(rootFolder))
                IO.Directory.Delete(rootFolder);
        }

        [TestInitialize()]
        public void Initialize()
        {
            IDbConnection conn = dbFactory.CreateDbConnection();
            conn.Open();

            manager = new FileManager(conn, rootFolder);
            Assert.IsNotNull(manager);

            root = manager.GetFolder("/");
        }

        [ClassCleanup()]
        public static void Cleanup()
        {
            //var dbConn = dbFactory.CreateDbConnection();
            //dbConn.Open();
            //dbConn.DeleteAll<FileBase>();
            //dbConn.DeleteAll<FileVersion>();
            //dbConn.DeleteAll<Folder>();


            //IO.Directory.Delete(rootFolder);
        }

        [TestMethod]
        public void BasicCreate()
        {
            Folder folder1;

            manager.CreateFolder(root, "BasicCreate", out folder1);
            Assert.IsNotNull(folder1);
            Assert.AreEqual<string>("/BasicCreate/", folder1.FullPath);
        }

        [TestMethod]
        public void BasicCreateDuplicate()
        {
            Folder folder;
            Result r;
            manager.CreateFolder(root, "BasicCreateDuplicate", out folder);
            r = manager.CreateFolder(root, "BasicCreateDuplicate", out folder);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);
            Assert.IsNull(folder);

            r = manager.CreateFolder(root, "BasicCreateDuplicate", OverwriteBehavior.Overwrite, out folder);
            Assert.AreEqual<Result>(r, Result.Success);

            r = manager.CreateFolder(root, "BasicCreateDuplicate", OverwriteBehavior.Skip, out folder);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);

            r = manager.CreateFolder(root, "BasicCreateDuplicate", OverwriteBehavior.Copy, out folder);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(folder.Name, "BasicCreateDuplicate - Copy");

        }

        [TestMethod]
        public void BasicCreateNested()
        {
            Folder folder;
            manager.CreateFolder(root, "BasicCreateNested", out folder);
            Result r = manager.CreateFolder(folder, "BasicCreateNested", out folder);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>("/BasicCreateNested/BasicCreateNested/", folder.FullPath);
        }

        [TestMethod]
        public void BasicCreateNestedDuplicate()
        {
            // Create /BasicCreateNestedDuplicate / BasicCreateNested.. and dupe that final one
            Folder folder, folder2;
            Result r;
            manager.CreateFolder(root, "BasicCreateNestedDuplicate", out folder);
            manager.CreateFolder(folder, "BasicCreateNestedDuplicate", out folder2);

            r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", out folder2);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);
            Assert.IsNull(folder2);

            r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", OverwriteBehavior.Overwrite, out folder2);
            Assert.AreEqual<Result>(r, Result.Success);

            r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", OverwriteBehavior.Skip, out folder2);
            Assert.AreEqual<Result>(r, Result.FolderAlreadyExists);

            r = manager.CreateFolder(folder, "BasicCreateNestedDuplicate", OverwriteBehavior.Copy, out folder2);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(folder2.Name, "BasicCreateNestedDuplicate - Copy");
        }

        [TestMethod]
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
        public void Move()
        {
            Folder folder1, folder2;
            Result r;

            manager.CreateFolder(root, "Move", out folder1);
            //manager.MoveFolder(folder1, )
        }
    }
}
