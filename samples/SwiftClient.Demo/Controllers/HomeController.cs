using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.OptionsModel;
using System.Collections.Generic;
using Microsoft.Framework.Caching.Memory;

namespace SwiftClient.Demo.Controllers
{
    public class HomeController : Controller
    {
        string containerTempId = "demotempcontainer";
        string containerId = "democontainer";
        SwiftClient client;

        public HomeController(IOptions<SwiftCredentials> credentials, IMemoryCache cache)
        {
            client = new SwiftClient(new SwiftAuthManagerWithCache(credentials.Value, cache));

            client.SetRetryCount(2)
                  .SetLogger(new SwiftLogger());
        }

        public async Task<IActionResult> Index()
        {
            await client.PutContainer(containerId);

            await client.PutContainer(containerTempId);

            var containerData = await client.GetContainer(containerId, null, new Dictionary<string, string> { { "format", "json" } });

            var viewModel = new ContainerViewModel();

            if (containerData.IsSuccess)
            {
                viewModel.Objects = containerData.Objects;
            }
            else
            {
                viewModel.Message = containerData.Reason;
            }

            return View(viewModel);
        }

        public async Task<IActionResult> UploadChunk(int segment)
        {
            if (Request.Form.Files != null && Request.Form.Files.Count > 0)
            {
                var file = Request.Form.Files[0];
                var fileStream = file.OpenReadStream();
                var memoryStream = new MemoryStream();
                var fileName = file.GetFileName();

                await fileStream.CopyToAsync(memoryStream);

                var resp = await client.PutChunkedObject(containerTempId, fileName, memoryStream.ToArray(), segment);

                return new JsonResult(new
                {
                    ContentType = file.ContentType,
                    FileName = fileName ?? "demofile",
                    Status = resp.StatusCode,
                    Message = resp.Reason,
                    Success = resp.IsSuccess
                });
            }

            return new JsonResult(new
            {
                Success = false
            });
        }

        public async Task<IActionResult> UploadDone(int segmentsCount, string fileName, string contentType)
        {
            // use manifest to merge chunks
            await client.PutManifest(containerTempId, fileName);

            // copy chunks to new file and set some meta data info about the file (filename, contentype)
            await client.CopyObject(containerTempId, fileName, containerId, fileName, new Dictionary<string, string>
                {
                    { string.Format(SwiftHeaderKeys.ObjectMetaFormat, "Filename"), fileName },
                    { string.Format(SwiftHeaderKeys.ObjectMetaFormat, "Contenttype"), contentType }
                });

            // cleanup temp chunks
            var deleteTasks = new List<Task>();

            for (var i = 0; i <= segmentsCount; i++)
            {
                deleteTasks.Add(client.DeleteObjectChunk(containerTempId, fileName, i));
            }

            // cleanup manifest
            deleteTasks.Add(client.DeleteObject(containerTempId, fileName));

            // cleanup temp container
            await Task.WhenAll(deleteTasks);

            return new JsonResult(new
            {
                Success = true
            });
        }

        public async Task<IActionResult> PlayVideo(string fileId)
        {
            var headObject = await client.HeadObject(containerId, fileId);

            if (headObject.IsSuccess)
            {
                var fileName = headObject.GetMeta("Filename");
                var contentType = headObject.GetMeta("Contenttype");

                var stream = new BufferedHTTPStream((start, end) =>
                {
                    var response = client.GetObjectRange(containerId, fileId, start, end).Result;

                    return response.Stream;

                }, () => headObject.ContentLength);

                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                return new FileStreamResult(stream, contentType);

                //return new VideoStreamResult(stream, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("video/mp4"));
            }

            return new HttpNotFoundResult();
        }

        public async Task<IActionResult> DownloadFile(string fileId)
        {
            var headObject = await client.HeadObject(containerId, fileId);

            if (headObject.IsSuccess && headObject.ContentLength > 0)
            {
                var fileName = headObject.GetMeta("Filename");
                var contentType = headObject.GetMeta("Contenttype");

                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                var stream = new BufferedHTTPStream((start, end) =>
                {
                    var response = client.GetObjectRange(containerId, fileId, start, end).Result;

                    return response.Stream;

                }, () => headObject.ContentLength);

                return new FileStreamResult(stream, contentType);
            }

            return new HttpNotFoundResult();
        }
    }
}
