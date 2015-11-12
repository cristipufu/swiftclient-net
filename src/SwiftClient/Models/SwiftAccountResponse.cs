using System.Collections.Generic;

namespace SwiftClient
{
    public class SwiftAccountResponse : SwiftBaseResponse
    {
        public long ContainersCount { get; set; }
        public long ObjectsCount { get; set; }
        public long TotalBytes { get; set; }
        public List<SwiftContainerModel> Containers { get; set; }
    }
}
