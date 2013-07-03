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
    public class FileTests
    {
        public const string dataFileFolder = @"c:\Temp\";

        FileManager manager;
        Folder root;

        [TestInitialize()]
        public void Initialize()
        {
            IDbConnection conn = GlobalInit.dbFactory.CreateDbConnection();
            conn.Open();

            manager = new FileManager(conn, GlobalInit.rootFolder);
            Assert.IsNotNull(manager);

            root = manager.GetFolder("/");
        }

        [TestMethod]
        [TestCategory("Files")]
        public void Create()
        {
            File file;
            Result r;
            IO.FileInfo fileInfo1, fileInfo2;

            fileInfo1 = new IO.FileInfo(dataFileFolder + "a.pdf");
            fileInfo2 = new IO.FileInfo(dataFileFolder + "b.pdf");


            using (IO.FileStream fs = new IO.FileStream(fileInfo1.FullName, IO.FileMode.Open))
                r = manager.CreateFile(root, "create1.pdf", fs, out file);
            Assert.AreEqual<Result>(r, Result.Success);

            file = manager.GetFile(root, "create1.pdf");
            Assert.IsNotNull(file);
            Assert.AreEqual<long>(file.Size, fileInfo1.Length);

            // Overwrite with a new file stream
            using (IO.FileStream fs = new IO.FileStream(fileInfo2.FullName, IO.FileMode.Open))
                r = manager.CreateFile(root, "create1.pdf", fs, out file);

            file = manager.GetFile(root, "create1.pdf");
            Assert.IsNotNull(file);
            Assert.AreEqual<long>(file.Size, fileInfo2.Length);
            Assert.AreEqual<int>(file.CurrentVersion, 2);
        }

        [TestMethod]
        [TestCategory("Files")]
        public void Read()
        {
            File file;
            Result r;
            IO.FileInfo fileInfo;
            IO.Stream outStream;

            fileInfo = new IO.FileInfo(dataFileFolder + "a.pdf");
            using (IO.FileStream fs = new IO.FileStream(fileInfo.FullName, IO.FileMode.Open))
                manager.CreateFile(root, "read1.pdf", fs, out file);

            r = manager.ReadFile(file, out outStream);
            Assert.AreEqual<Result>(r, Result.Success);
            long fileLength = 0;
            long read = -1;
            byte[] buffer = new byte[1024 * 4];
            while ((read = outStream.Read(buffer, 0, buffer.Length)) > 0)
                fileLength += read;
            outStream.Close();

            Assert.AreEqual<long>(fileInfo.Length, fileLength);
        }

        [TestMethod]
        [TestCategory("Files")]
        public void Copy()
        {
            File file, fileCopy;

            Folder folder;
            Result r;
            IO.FileInfo fileInfo;

            fileInfo = new IO.FileInfo(dataFileFolder + "a.pdf");
            using (IO.FileStream fs = new IO.FileStream(fileInfo.FullName, IO.FileMode.Open))
                manager.CreateFile(root, "copy1.pdf", fs, out file);

            manager.CreateFolder(root, "copy", out folder);

            // Simple copy test
            r = manager.CopyFile(file, folder, out fileCopy);
            Assert.AreEqual<Result>(r, Result.Success);

            // Check for conflict
            r = manager.CopyFile(file, folder, OverwriteBehavior.RaiseConflict, out fileCopy);
            Assert.AreEqual<Result>(r, Result.FileAlreadyExists);

            // Create a copy
            r = manager.CopyFile(file, folder, OverwriteBehavior.Copy, out fileCopy);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(fileCopy.Name, "copy1 - Copy.pdf");
            fileCopy = manager.GetFile(folder, "copy1 - Copy.pdf");
            Assert.IsNotNull(fileCopy);

            // Specify a new name explicity
            r = manager.CopyFile(file, folder, "copy2.pdf", OverwriteBehavior.RaiseConflict, out fileCopy);
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(fileCopy.Name, "copy2.pdf");
            fileCopy = manager.GetFile(folder, "copy2.pdf");
            Assert.IsNotNull(fileCopy);

        }

        [TestMethod]
        [TestCategory("Files")]
        public void Move()
        {
            File file, fileCopy;

            Folder folder;
            Result r;
            IO.FileInfo fileInfo;

            fileInfo = new IO.FileInfo(dataFileFolder + "a.pdf");
            using (IO.FileStream fs = new IO.FileStream(fileInfo.FullName, IO.FileMode.Open))
                manager.CreateFile(root, "move1.pdf", fs, out file);

            manager.CreateFolder(root, "move", out folder);
            r = manager.MoveFile(file, folder);
            Assert.AreEqual<Result>(r, Result.Success);

            manager.CopyFile(file, root, out fileCopy);

            // Copy over again, should fail
            r = manager.MoveFile(fileCopy, folder);
            Assert.AreEqual<Result>(r, Result.FileAlreadyExists);

            // Copy over again, with copy behavior
            r = manager.MoveFile(fileCopy, folder, OverwriteBehavior.Copy);
            Assert.AreEqual<Result>(r, Result.Success);
            fileCopy = manager.GetFile(folder, "move1 - Copy.pdf");
            Assert.IsNotNull(fileCopy);

        }

        [TestMethod]
        [TestCategory("Files")]
        public void Rename()
        {
            File file, fileCopy;

            Result r;
            IO.FileInfo fileInfo;

            fileInfo = new IO.FileInfo(dataFileFolder + "a.pdf");
            using (IO.FileStream fs = new IO.FileStream(fileInfo.FullName, IO.FileMode.Open))
                manager.CreateFile(root, "rename1.pdf", fs, out file);
            manager.CopyFile(file, root, "rename2.pdf", OverwriteBehavior.RaiseConflict, out fileCopy);

            r = manager.RenameFile(fileCopy, "rename3.pdf");
            Assert.AreEqual<Result>(r, Result.Success);
            Assert.AreEqual<string>(fileCopy.Name, "rename3.pdf");
            Assert.IsNotNull(manager.GetFile(root, "rename3.pdf"));

            r = manager.RenameFile(fileCopy, "rename1.pdf");
            Assert.AreEqual<Result>(r, Result.FileAlreadyExists);
            
        }

        [TestMethod]
        [TestCategory("Files")]
        public void DeleteAndRestore()
        {
            File file, fileCopy;

            Result r;
            IO.FileInfo fileInfo;

            fileInfo = new IO.FileInfo(dataFileFolder + "a.pdf");
            using (IO.FileStream fs = new IO.FileStream(fileInfo.FullName, IO.FileMode.Open))
                manager.CreateFile(root, "delete1.pdf", fs, out file);
            manager.CopyFile(file, root, "delete2.pdf", OverwriteBehavior.RaiseConflict, out fileCopy);

            r = manager.DeleteFile(file);
            
            r = manager.MoveFile(fileCopy, root, "delete1.pdf", OverwriteBehavior.RaiseConflict);
            r = manager.RestoreFile(file);  //should fail


        }
    }
}
