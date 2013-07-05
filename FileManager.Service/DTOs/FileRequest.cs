using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace FileManager.Service.DTOs
{
    /// <summary>
    /// Incoming file-related requests
    /// </summary>
    [Route("/files")]
    [Route("/files/{Path*}")]
    public class FileRequest
    {
        public string Path { get; set; }
        public bool ForDownload { get; set; }

        public FileRequest()
        {
            ForDownload = true;
        }
    }


    /// <summary>
    /// The response to most File-related requests.  Because a path can be ambiguous, this contains both a FileResult and a FolderResult
    /// </summary>
    public class FileResponse : IHasResponseStatus
    {
        public FileResult File { get; set; }
        public FolderResult Folder { get; set; }

        public string MyProperty { get { return "test"; }  }

        public ResponseStatus ResponseStatus { get; set; }
    }
}
