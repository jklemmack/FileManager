using System;
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
    public class GlobalInit
    {
        public const string connectionString = @"Data Source=ORB515720\SQLExpress;Initial Catalog=FileManager;Integrated Security=True";
        public const string rootFolder = @"C:\Temp\Files\";


        public static OrmLiteConnectionFactory dbFactory;

        [AssemblyInitialize()]
        public static void AssemblyInitialize(TestContext context)
        {
            dbFactory = new OrmLiteConnectionFactory(connectionString, SqlServerOrmLiteDialectProvider.Instance);
            var dbConn = dbFactory.CreateDbConnection();

            dbConn.Open();
            // Drop & create the tables
            dbConn.DropAndCreateTable<FileBase>();
            dbConn.DropAndCreateTable<FileVersion>();
            dbConn.DropAndCreateTable<Folder>();

            //dbConn.DeleteAll<FileVersion>();
            //dbConn.DeleteAll<FileBase>();
            //dbConn.DeleteAll<Folder>();

            dbConn.Close();

            if (IO.Directory.Exists(rootFolder))
                IO.Directory.Delete(rootFolder, true);

            IO.Directory.CreateDirectory(rootFolder);
        }
    }
}
