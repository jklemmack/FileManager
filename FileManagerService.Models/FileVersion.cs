using System;
using ServiceStack.DataAnnotations;

namespace FileManagerService.Models
{
    [CompositeIndex(true, new string[]{"FileId", "VersionId"})]
    public class FileVersion
    {
        public long FileId { get; set; }
        public long VersionId { get; set; }
        
        public int Version { get; set; }
        public bool IsCurrent { get; set; }
        public Guid FileStore { get; set; }

        public bool IsDeleted { get; set; }
    }
}
