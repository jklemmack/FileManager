using System;
using ServiceStack.DataAnnotations;

namespace FileManager.Models
{
    [CompositeIndex(true, new string[]{"FileId", "VersionId"})]
    public class FileVersion
    {
        [PrimaryKey()]
        [AutoIncrement()]
        public long VersionId { get; set; }

        //[References(typeof(File))]
        public long FileId { get; set; }
        
        public int Version { get; set; }
        public bool IsCurrent { get; set; }
        public Guid FileStore { get; set; }
        public long Size { get; set; }

        public bool IsDeleted { get; set; }
    }
}
