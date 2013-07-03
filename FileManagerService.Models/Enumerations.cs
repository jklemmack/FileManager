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
        FolderIsNotDeleted,
        FileIsNotDeleted,
        FileStoreNotFound,
        CannotModifyDeletedItems,
        GeneralFailure
    }

    public enum OverwriteBehavior
    {
        RaiseConflict,
        //Overwrite,
        Copy
    }

    public enum ReadType
    {
        Default = 1,
        OnlyNonDeleted = 1,
        OnlyDeleted = 2,
        All = 0
    }

}
