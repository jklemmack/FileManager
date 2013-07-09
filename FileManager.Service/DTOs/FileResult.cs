using System;
using System.Collections.Generic;
using FileManager.Models;

namespace FileManager.Service.DTOs
{
    public class FileResult 
    {
        public long FileId { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime LastModifiedTimestamp { get; set; }

        public FileResult()
        {}

        public FileResult(File file)
        {
            this.FileId = file.Id;
            this.Name = file.Name;
            this.LastModifiedTimestamp = LastModifiedTimestamp;
        }
    }

    public class FolderResult
    {
        public List<File> Files { get; set; }
        public List<Folder> Folders { get; set; }

        public FolderResult()
        {
            Files = new List<File>();
            Folders = new List<Folder>();
        }

        public static FolderResult Create(IEnumerable<Folder> folders, IEnumerable<File> files)
        {
            FolderResult result = new FolderResult();
            result.Files = new List<File>(files);
            result.Folders = new List<Folder>(folders);
            return result;
        }
    }
}
