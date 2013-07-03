using System;
using ServiceStack.DataAnnotations;

namespace FileManagerService.Models
{
    public class Folder : IHierarchyItem
    {
        [PrimaryKey]
        [AutoIncrement]
        [Alias("FolderId")]
        public long Id { get; set; }


        [Index()]
        public string FullPath { get; set; }

        public string Name { get; set; }

        //[References(typeof(Folder))]
        public long? ParentFolderId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPurged { get; set; }
        public DateTime CreatedTimeStamp { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }

        public override string ToString()
        {
            return FullPath;
        }
    }
}
