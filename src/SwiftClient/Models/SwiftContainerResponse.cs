using System.Collections.Generic;

namespace SwiftClient
{
    public class SwiftContainerResponse : SwiftBaseResponse
    {
        public long ObjectsCount { get; set; }
        public long TotalBytes { get; set; }
        public List<SwiftObjectModel> Objects { get; set; }

    }
}
