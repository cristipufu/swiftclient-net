using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.OptionsModel;

namespace SwiftClient.Demo.Controllers
{
    public class HomeController : Controller
    {
        string containerId = "democontainer";
        string objectId = "demo-video";

        SwiftClient client;

        public HomeController(IOptions<SwiftCredentials> credentials)
        {
            client = new SwiftClient(credentials.Value);

            client.SetRetryCount(2)
                  .SetLogger(new SwiftLogger());

            client.PutContainer(containerId);
        }

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

                await client.PutChunkedObject(containerId, objectId, memoryStream.ToArray(), segment);

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
            await client.PutManifest(containerId, objectId);

            return new JsonResult(new
            {
                Success = true
            });
        }
    }
}
