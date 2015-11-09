using System.IO;

namespace SwiftClient
{
    public class SwiftResponse : SwiftBaseResponse
    {
        public Stream Stream { get; set; }
    }
}
