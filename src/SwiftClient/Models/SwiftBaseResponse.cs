using System.Collections.Generic;
using System.Net;

namespace SwiftClient
{
    public class SwiftBaseResponse
    {
        public HttpStatusCode StatusCode { get; set; }

        public string Reason { get; set; }

        public long ContentLength { get; set; }

        public bool IsSuccess { get; set; }

        public Dictionary<string, string> Headers { get; set; }
    }
}
