using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.IO;

namespace SwiftClient.Demo.Controllers
{
    public class HomeController : Controller
    {
        private string _containerName = "democontainer";
        private string _objectName = "demo-video";

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> UploadChunk(int segment)
        {
            if (Request.Body != null && Request.Body.CanRead)
            {
                var memoryStream = new MemoryStream();

                Request.Body.CopyTo(memoryStream);

                SwiftClient client = GetSwiftClient();

                await client.PutChunkedObject(_containerName, _objectName, memoryStream.ToArray(), segment);

                return new JsonResult(new
                {
                    Success = true
                });
            }

            return new JsonResult(new
            {
                Success = false
            });
        }

        public async Task<IActionResult> UploadDone()
        {
            SwiftClient client = GetSwiftClient();

            await client.PutManifest(_containerName, _objectName);

            return new JsonResult(new
            {
                Success = true
            });
        }

        private SwiftClient GetSwiftClient()
        {
            SwiftClient client = new SwiftClient();

            client.WithCredentials(new SwiftCredentials
            {
                Username = "preview:root",
                Password = "swift@VT!@#",
                Endpoints = new List<string> { "http://192.168.3.21:8080" }
            })
            .SetRetryCount(2);

            client.PutContainer(_containerName);

            return client;
        }

    }
}
