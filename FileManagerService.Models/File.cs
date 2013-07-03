using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace FileManagerService.Models
{
    [Alias("File")]
    public class FileBase : IHierarchyItem
    {
        [PrimaryKey]
        [AutoIncrement]
        [Alias("FileId")]
        public long Id { get; set; }
        public string Name { get; set; }

        //[References(typeof(Folder))]
        public long? ParentFolderId { get; set; }
        [Ignore]
        public string FullPath { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPurged { get; set; }
        public DateTime CreatedTimeStamp { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }

        public override string ToString()
        {
            return FullPath;
        }
    }

    // Join of File & File Version.  Only used for communication outside the service
    public class File : IHierarchyItem
    {
        //[References(typeof(FileBase))]
        public long Id { get; set; }
        public string Name { get; set; }
        public long? ParentFolderId { get; set; }
        public string FullPath { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPurged { get; set; }
        public DateTime CreatedTimeStamp { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }

        public Guid FileStore { get; set; }
        public long Size { get; set; }
        public int CurrentVersion { get; set; }

        public override string ToString()
        {
            return FullPath;
        }
        //public File()
        //{

        //}

        //public File(FileBase fileBase, FileVersion fileVersion)
        //{
        //    this.Id = fileBase.Id;
        //    this.Name = fileBase.Name;
        //    this.ParentFolderId = fileBase.ParentFolderId;
        //    this.IsDeleted = fileBase.IsDeleted;
        //    this.IsPurged = fileBase.IsPurged;
        //    this.CreatedTimeStamp = fileBase.CreatedTimeStamp;
        //    this.LastModifiedTimestamp = fileBase.LastModifiedTimestamp;

        //    this.FileStore = fileVersion.FileStore;
        //    this.CurrentVersion = fileVersion.Version;
        //}
    }
}
