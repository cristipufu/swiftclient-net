using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.OptionsModel;
using System.Collections.Generic;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace SwiftClient.Demo.Controllers
{
    public class HomeController : Controller
    {
        string containerTempId = "demotempcontainer";
        string containerDemoId = "democontainer";

        string metaFileName = "Filename";
        string metaContentType = "Contenttype";

        SwiftCredentials Credentials;
        Client Client;

        public HomeController(IOptions<SwiftCredentials> credentials, IMemoryCache cache)
        {
            Credentials = credentials.Value;

            Client = new Client(new SwiftAuthManagerWithCache(Credentials, cache));

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

                viewModel.Tree = await GetTree();
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
                var fileName = file.GetFileName();

                using (var fileStream = file.OpenReadStream())
                {
                    using (var memoryStream = new MemoryStream())
                    {
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
                }
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
            await Client.CopyObject(containerTempId, fileName, containerDemoId, fileName, new Dictionary<string, string>
            {
                { $"X-Object-Meta-{metaFileName}", fileName },
                { $"X-Object-Meta-{metaContentType}", contentType }
            });

            // cleanup temp
            await Client.DeleteContainerContents(containerTempId);

            return new JsonResult(new
            {
                Success = true
            });
        }

        public async Task<IActionResult> PlayVideo(string containerId, string objectId)
        {
            var headObject = await Client.HeadObject(containerId, objectId);

            if (headObject.IsSuccess)
            {
                var fileName = headObject.GetMeta(metaFileName);
                var contentType = headObject.GetMeta(metaContentType);

                var stream = new BufferedHTTPStream((start, end) =>
                {
                    var response = Client.GetObjectRange(containerId, objectId, start, end).Result;

                    return response.Stream;

                }, () => headObject.ContentLength);

                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                return new VideoStreamResult(stream, new MediaTypeHeaderValue("video/mp4"));
            }

            return new HttpNotFoundResult();
        }

        public async Task<IActionResult> DownloadFile(string containerId, string objectId)
        {
            var headObject = await Client.HeadObject(containerId, objectId);

            if (headObject.IsSuccess && headObject.ContentLength > 0)
            {
                var fileName = headObject.GetMeta(metaFileName);
                var contentType = headObject.GetMeta(metaContentType);

                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                var stream = new BufferedHTTPStream((start, end) =>
                {
                    var response = Client.GetObjectRange(containerId, objectId, start, end).Result;

                    return response.Stream;

                }, () => headObject.ContentLength);

                return new FileStreamResult(stream, contentType ?? "application/octet-stream");
            }

            return new HttpNotFoundResult();
        }

        public async Task<IActionResult> RefreshTree()
        {
            return new JsonResult(new
            {
                Data = await GetTree()
            });
        }

        private async Task<TreeViewModel> GetTree()
        {
            var tree = new TreeViewModel();

            var accountData = await Client.GetAccount();

            if (accountData.IsSuccess)
            {
                tree.Text = Credentials.Username;

                if (accountData.Containers != null)
                {
                    tree.Nodes = new List<TreeViewModel>();

                    var tasks = new List<Task<TreeViewModel>>();

                    foreach (var container in accountData.Containers)
                    {
                        tasks.Add(GetContainerBranch(container.Container));
                    }

                    await Task.WhenAll(tasks).ContinueWith((rsp) =>
                    {
                        tree.Nodes.AddRange(rsp.Result);
                    });
                }
            }

            return tree;
        }

        private async Task<TreeViewModel> GetContainerBranch(string containerId)
        {
            var containerData = await Client.GetContainer(containerId);

            TreeViewModel result = new TreeViewModel
            {
                Text = containerId,
                ContainerId = containerId
            };

            if (containerData.IsSuccess)
            {
                if (containerData.Objects != null && containerData.ObjectsCount > 0)
                {
                    result.Nodes = GetObjectBranch(containerId, "", containerData.Objects.Select(x => x.Object).ToList()).ToList();

                    if (result.Nodes != null && result.Nodes.Any())
                    {
                        result.HasNodes = true;
                    }
                }
            }

            return result;
        }

        private List<TreeViewModel> GetObjectBranch(string containerId, string prefixObj, List<string> objectIds)
        {
            var prefixes = objectIds.Select(x => x.Split('\\')[0]).Distinct().ToList();

            List<TreeViewModel> result = null;

            if (prefixes.Any())
            {
                result = new List<TreeViewModel>();

                foreach (var prefix in prefixes)
                {
                    var newPrefix = !string.IsNullOrEmpty(prefixObj) ? prefixObj + "\\" + prefix : prefix;

                    var tree = new TreeViewModel
                    {
                        ObjectId = newPrefix,
                        ContainerId = containerId,
                        Text = prefix
                    };

                    var prefixedObjs = objectIds.Where(x => x.StartsWith(prefix + "\\")).Select(x => x.Split(new[] { '\\' }, 2)[1]).ToList();

                    tree.Nodes = GetObjectBranch(containerId, newPrefix, prefixedObjs);

                    if (tree.Nodes == null) { tree.IsFile = true; }

                    result.Add(tree);
                }
            }

            return result;

        }
    }
}
