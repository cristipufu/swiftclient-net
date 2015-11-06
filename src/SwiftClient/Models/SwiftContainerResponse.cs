
namespace SwiftClient.Models
{
    public class SwiftContainerResponse : SwiftBaseResponse
    {
        public long ObjectsCount { get; set; }
        public long TotalBytes { get; set; }
        public string Info { get; set; }

    }
}
