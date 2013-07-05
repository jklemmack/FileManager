using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

using System.Data;
using ServiceStack.Configuration;
using ServiceStack.MiniProfiler;
using ServiceStack.OrmLite;
using ServiceStack.WebHost.Endpoints;

namespace FileManager.Web
{
    public class FileManagerHost : AppHostBase
    {

        public FileManagerHost()
            : base("REST Files", typeof(FileManager.Service.FilesService).Assembly)
        {
        }

        public override void Configure(Funq.Container container)
        {
            Plugins.Add(new ServiceStack.Razor.RazorFormat());

            SetConfig(new EndpointHostConfig
            {
                GlobalResponseHeaders = {
                    {"Access-Control-Allow-Origin", "*"},
                    {"Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS"}
                },
            });

            var config = new FileManager.Service.AppConfig(new ConfigurationResourceManager());
            container.Register(config);

            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(@"Data Source=ORB515720\SQLExpress;Initial Catalog=FileManager;Integrated Security=True"
                                            , ServiceStack.OrmLite.SqlServer.SqlServerOrmLiteDialectProvider.Instance)
                {
                    ConnectionFilter = x => new ServiceStack.MiniProfiler.Data.ProfiledDbConnection(x, Profiler.Current)
                });

        }
    }

    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            (new FileManagerHost()).Init();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}