using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerService.Models
{
    public enum Result
    {
        Success = 0,
        InvalidFolderPath,
        FolderAlreadyExists,
        FileAlreadyExists,
        FolderNotFound,
        FileNotFound,
        TargetIsChildOfSource,
        GeneralFailure
    }

    public enum OverwriteBehavior
    {
        Skip,
        //RaiseConflict,
        Overwrite,
        Copy
    }

    public enum ReadType
    {
        Default,
        OnlyDeleted,
        All
    }
}
