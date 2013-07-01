using System;
using System.Collections.Generic;
using IO = System.IO;

using FileManagerService.Models;

namespace FileManagerService.Interfaces
{
    public interface IFileManager
    {
        Folder GetFolder(string path);
        File GetFile(string path);

        IEnumerable<IHierarchyItem> GetAllItems(string path);
        IEnumerable<IHierarchyItem> GetAllChanges(DateTime sinceTimestamp);

        Result GetChildren(Folder folder, out IEnumerable<Folder> folders, out IEnumerable<File> files);
        Result GetChildren(Folder folder, ReadType readType, out IEnumerable<Folder> folders, out IEnumerable<File> files);

        Result CreateFolder(Folder parentFolder, string name, out Folder folder);
        Result CopyFolder(Folder source, Folder target, OverwriteBehavior behavior);
        Result MoveFolder(Folder source, Folder target, OverwriteBehavior behavior);
        Result RenameFolder(Folder source, string name);
        Result DeleteFolder(Folder source);
        Result RestoreFolder(Folder source);
        Result PurgeFolder(Folder source);

        Result ReadFile(File file, out IO.Stream stream);
        Result CreateFile(Folder folder, string name, IO.Stream stream);
        Result CopyFile(File source, string target, OverwriteBehavior behavior);
        Result MoveFile(File source, string target, OverwriteBehavior behavior);
        Result DeleteFile(File source);
        Result RestoreFile(File source);
        Result PurgeFile(File source);

    }
}
