using System;
using System.Collections.Generic;
using FileManager;
using FileManager.Service.DTOs;

namespace FileManager.Service
{
    public class FilesService : ServiceStack.ServiceInterface.Service
    {
        public AppConfig Config { get; set; }

        public FileResponse Get(FileRequest request)
        {
            string path = GetPath(request.Path);

            // See if it's a folder
            Manager manager = new Manager(base.Db, Config.RootDirectory);
            Models.Folder folder = manager.GetFolder(path);

            if (folder != null)
            {
                IEnumerable<Models.Folder> folders = null;
                IEnumerable<Models.File> files = null;

                manager.GetChildren(folder, out folders, out files);
                FileResponse response = new FileResponse();
                response.Directory = FolderResult.Create(folders, files);

                return response;
            }


            return new FileResponse();
        }

        private string GetPath(string path)
        {
            path = GetSafePath(path);
            path = "/" + path;
            return path;
        }

        private string GetSafePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            return path.TrimStart('.', '/', '\\')
                        .Replace('\\', '/')
                        ;

        }

    }



}
