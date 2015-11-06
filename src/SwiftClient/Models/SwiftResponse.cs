using System.IO;

namespace SwiftClient.Models
{
    public class SwiftResponse : SwiftBaseResponse
    {
        public Stream Stream { get; set; }
    }
}
