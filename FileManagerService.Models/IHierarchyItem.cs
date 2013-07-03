using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerService.Models
{
    public interface IHierarchyItem
    {
         long Id { get; set; }
         string Name { get; set; }
         long? ParentFolderId { get; set; }
         string FullPath { get; set; }

         bool IsDeleted { get; set; }
         bool IsPurged { get; set; }

         DateTime CreatedTimeStamp { get; set; }
         DateTime LastModifiedTimestamp { get; set; }
    }
}
