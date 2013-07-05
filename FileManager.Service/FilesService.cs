using System;
using FileManager;
using FileManager.Service.DTOs;

namespace FileManager.Service
{
    public class FilesService : ServiceStack.ServiceInterface.Service
    {
        public AppConfig Config { get; set; }

        public FileResponse Get(FileRequest request)
        {
            string path = request.Path;

            Manager manager = new Manager(base.Db, Config.RootDirectory);
            
            
            return new FileResponse();
        }
    }



}
