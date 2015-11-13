using System.Threading.Tasks;
using System.IO;
using System.Linq;
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

        string metaFileName = "Filename";
        string metaContentType = "Contenttype";

        SwiftCredentials Credentials;
        SwiftClient Client;

        public HomeController(IOptions<SwiftCredentials> credentials, IMemoryCache cache)
        {
            Credentials = credentials.Value;

            Client = new SwiftClient(new SwiftAuthManagerWithCache(Credentials, cache));

            Client.SetRetryCount(2)
                  .SetLogger(new SwiftLogger());
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new PageViewModel();

            var authData = await Client.Authenticate();

            if (authData != null)
            {
                viewModel.Message = $"Connected on proxy node: {authData.StorageUrl} with authentication token: {authData.AuthToken}";

                await Client.PutContainer(containerId);

                await Client.PutContainer(containerTempId);

                viewModel.Tree = new List<TreeViewModel> { await GetTree() };
            }
            else
            {
                viewModel.Message = $"Error connecting to proxy node: {Credentials.Endpoints.First()} with credentials: {Credentials.Username} / {Credentials.Password}";
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

                var resp = await Client.PutObjectChunk(containerTempId, fileName, memoryStream.ToArray(), segment);

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
            await Client.PutManifest(containerTempId, fileName);

            // copy chunks to new file and set some meta data info about the file (filename, contentype)
            await Client.CopyObject(containerTempId, fileName, containerId, fileName, new Dictionary<string, string>
            {
                { $"X-Object-Meta-{metaFileName}", fileName },
                { $"X-Object-Meta-{metaContentType}", contentType }
            });

            // cleanup temp
            await Client.DeleteContainerContents(containerId);

            return new JsonResult(new
            {
                Success = true
            });
        }

        public async Task<IActionResult> PlayVideo(string fileId)
        {
            var headObject = await Client.HeadObject(containerId, fileId);

            if (headObject.IsSuccess)
            {
                var fileName = headObject.GetMeta(metaFileName);
                var contentType = headObject.GetMeta(metaContentType);

                var stream = new BufferedHTTPStream((start, end) =>
                {
                    var response = Client.GetObjectRange(containerId, fileId, start, end).Result;

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
            var headObject = await Client.HeadObject(containerId, fileId);

            if (headObject.IsSuccess && headObject.ContentLength > 0)
            {
                var fileName = headObject.GetMeta(metaFileName);
                var contentType = headObject.GetMeta(metaContentType);

                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                var stream = new BufferedHTTPStream((start, end) =>
                {
                    var response = Client.GetObjectRange(containerId, fileId, start, end).Result;

                    return response.Stream;

                }, () => headObject.ContentLength);

                return new FileStreamResult(stream, contentType);
            }

            return new HttpNotFoundResult();
        }

        private async Task<TreeViewModel> GetTree()
        {
            var tree = new TreeViewModel();

            var accountData = await Client.GetAccount();

            if (accountData.IsSuccess)
            {
                tree.text = Credentials.Username;

                if (accountData.Containers != null)
                {
                    tree.nodes = new List<TreeViewModel>();

                    foreach (var container in accountData.Containers)
                    {
                        tree.nodes.Add(new TreeViewModel
                        {
                            text = container.Container,
                            nodes = await GetContainerObjects(container.Container)
                        });
                    }

                }
            }

            return tree;
        }

        private async Task<List<TreeViewModel>> GetContainerObjects(string containerId)
        {
            var containerData = await Client.GetContainer(containerId);

            List<TreeViewModel> result = null;

            if (containerData.IsSuccess)
            {
                if (containerData.Objects != null && containerData.ObjectsCount > 0)
                {
                    result = GetObjectNodes(containerData.Objects.Select(x => x.Object).ToList()).ToList();
                }
            }

            return result;
        }

        private List<TreeViewModel> GetObjectNodes(List<string> objectIds)
        {
            var prefixes = objectIds.Select(x => x.Split('/')[0]).Distinct().ToList();

            List<TreeViewModel> result = null;

            if (prefixes.Any())
            {
                result = new List<TreeViewModel>();

                foreach (var prefix in prefixes)
                {
                    var tree = new TreeViewModel
                    {
                        text = prefix
                    };

                    var prefixedObjs = objectIds.Where(x => x.StartsWith(prefix + "/")).Select(x => x.Split('/')[1]).ToList();

                    tree.nodes = GetObjectNodes(prefixedObjs);

                    result.Add(tree);
                }
            }

            return result;

        }
    }
}
