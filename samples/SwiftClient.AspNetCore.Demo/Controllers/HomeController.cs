using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using Microsoft.Extensions.Options;

namespace SwiftClient.AspNetCore.Demo.Controllers
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
            await Client.DeleteContainerWithContents(containerTempId);

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
                    using (var response = Client.GetObjectRange(containerId, objectId, start, end).Result)
                    {
                        var ms = new MemoryStream();

                        response.Stream.CopyTo(ms);

                        return ms;
                    }

                }, () => headObject.ContentLength);

                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                return new VideoStreamResult(stream, "video/mp4");
            }

            return new NotFoundResult();
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
                    using (var response = Client.GetObjectRange(containerId, objectId, start, end).Result)
                    {
                        var ms = new MemoryStream();

                        response.Stream.CopyTo(ms);

                        return ms;
                    }

                }, () => headObject.ContentLength);

                return new FileStreamResult(stream, contentType ?? "application/octet-stream");
            }

            return new NotFoundResult();
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
                tree.text = Credentials.Username;

                if (accountData.Containers != null)
                {
                    tree.nodes = new List<TreeViewModel>();

                    var tasks = new List<Task<TreeViewModel>>();

                    foreach (var container in accountData.Containers)
                    {
                        tasks.Add(GetContainerBranch(container.Container));
                    }

                    await Task.WhenAll(tasks).ContinueWith((rsp) =>
                    {
                        tree.nodes.AddRange(rsp.Result);
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
                text = containerId,
                containerId = containerId
            };

            if (containerData.IsSuccess)
            {
                if (containerData.Objects != null && containerData.ObjectsCount > 0)
                {
                    result.nodes = GetObjectBranch(containerId, "", containerData.Objects.Select(x => x.Object).ToList()).ToList();

                    if (result.nodes != null && result.nodes.Any())
                    {
                        result.hasNodes = true;
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
                        objectId = newPrefix,
                        containerId = containerId,
                        text = prefix
                    };

                    var prefixedObjs = objectIds.Where(x => x.StartsWith(prefix + "\\")).Select(x => x.Split(new[] { '\\' }, 2)[1]).ToList();

                    tree.nodes = GetObjectBranch(containerId, newPrefix, prefixedObjs);

                    if (tree.nodes == null)
                    {
                        tree.isExpandable = false;

                        if (tree.objectId.EndsWith(".mp4"))
                        {
                            tree.isVideo = true;
                        }
                        else
                        {
                            tree.isFile = true;
                        }
                    }
                    else
                    {
                        tree.isExpandable = true;
                    }

                    result.Add(tree);
                }
            }

            return result;

        }
    }
}
