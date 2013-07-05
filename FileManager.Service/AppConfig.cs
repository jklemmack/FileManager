using System;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;

namespace FileManager.Service
{
    public class AppConfig
    {
        public string RootDirectory { get; set; }

        public AppConfig()
        {

        }

        public AppConfig(IResourceManager resources)
        {
            this.RootDirectory = resources.GetString("RootDirectory").MapHostAbsolutePath();
        }
    }
}
