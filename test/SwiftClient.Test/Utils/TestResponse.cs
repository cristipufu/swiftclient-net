using System.Net;

namespace SwiftClient.Test
{
    public class TestResponse
    {
        public byte[] Bytes { get; set; }

        public long Length { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
