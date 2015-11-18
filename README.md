# OpenStack Swift client for .NET

[![Build Status](https://img.shields.io/teamcity/http/tcoss.veritech.io/e/SwiftClient_Ubuntu.svg?label=Ubuntu build)]
(http://tcoss.veritech.io/viewType.html?buildTypeId=SwiftClient_Ubuntu&guest=1)
[![Build Status](https://img.shields.io/teamcity/http/tcoss.veritech.io/e/SwiftClient_Windows.svg?label=Windows build)]
(http://tcoss.veritech.io/viewType.html?buildTypeId=SwiftClient_Windows&guest=1)
[![NuGet version](https://img.shields.io/nuget/vpre/SwiftClient.svg)](https://www.nuget.org/packages/SwiftClient/)

SwiftClient is an async HTTP wrapper over OpenStack Swift REST API and follows the [Object Storage API Reference](http://developer.openstack.org/api-ref-objectstorage-v1.html). 
It can be installed via NuGet from [nuget.org/packages/SwiftClient](https://www.nuget.org/packages/SwiftClient/) and it's compatible with .NET Framework 4.5, DNX 4.5.1 and DNXCore 5.0.

### Usage

The client implements a configurable retry mechanism, so you don't have to worry about the token expiration date or a temporary request failure. 
It also supports multiple endpoints (Swift proxy address), it will iterate throw each endpoint till it finds one that's available, if the maximum retry count is reached an exception will be thrown.
If you want to log failure events, just pass the client your implementation of the `ISwiftLogger` interface. In the demo project there is a [stdout log example](https://github.com/vtfuture/SwiftClient/blob/master/samples/SwiftClient.Demo/SwiftLogger.cs).

```cs
var swiftClient = new SwiftClient()
.WithCredentials(new SwiftCredentials
{
     Username = "test:tester",
     Password = "testing",
     Endpoints = new List<string> { 
		"http://192.168.3.31:8080",
		"http://192.168.3.32:8080"
		}
})
.SetRetryCount(6)
.SetRetryPerEndpointCount(2)
.SetLogger(new SwiftLogger());
```

You have to supply your own implementation of `ISwiftAuthManager` class and provide a caching mechanism for the ***authentication token*** so that each Swift request is not being preceded by an authentication request. It is recommended to use a dedicated cache storage like Redis so multiple instances of your app can reuse the authentication token. In the demo project there is a `ISwiftAuthManager` [implementation](https://github.com/vtfuture/SwiftClient/blob/master/samples/SwiftClient.Demo/SwiftAuthManagerWithCache.cs) that uses aspnet5 in memory cache.

### Running the ASP.NET 5 MVC demo

The [SwiftClient.Demo](https://github.com/vtfuture/SwiftClient/tree/master/src/SwiftClient.Demo) project is an example of how to authenticate against Swift, do chunked upload for a large file and download it. 

You will need at least one Ubuntu 14.04 box to host OpenStack Swift proxy and storage. For dev/test environments we provide a docker image with a single Swift proxy and storage, follow the setup instruction from [docker-swift](https://github.com/vtfuture/SwiftClient/tree/master/docker-swift) to build and run the Swift container. After you've started the Swift all-in-one container, put your Ubuntu box IP in the `appsettings.json` from the demo project and your good to go.

If you want to setup Swift for production on a Ubuntu cluster check out the [documentation](https://github.com/vtfuture/SwiftClient/wiki) from our wiki.

### ASP.NET 5 usage

You can load Swift credentials from an json file in aspnet5 project. Add an [`appsettings.json`](https://github.com/vtfuture/SwiftClient/blob/master/samples/SwiftClient.Demo/appsettings.json) file in the root your project and load the settings in [`Startup.cs`](https://github.com/vtfuture/SwiftClient/blob/master/samples/SwiftClient.Demo/Startup.cs).

Create a new instance of `SwiftClient.Client` using the specified `SwiftCredentials` in your controller:

```cs
public class HomeController : Controller
{
        SwiftClient.Client client;

        public HomeController(IOptions<SwiftCredentials> credentials)
        {
            client = new SwiftClient.Client(credentials.Value);

            client.SetRetryCount(2)
                  .SetLogger(new SwiftLogger());

        }
}
```

Simple upload/download example for small files
```cs
public async Task<IActionResult> UploadFile(IFormFile file)
{ 
    using (var fileStream = file.OpenReadStream())
    {
        using (var memoryStream = new MemoryStream())
        {
            await fileStream.CopyToAsync(memoryStream);

            var resp = await client.PutObject(containerId, fileId, memoryStream);

            return new JsonResult(new
            {
                Success = resp.IsSuccess
            });
        }
    }
}

public async Task<IActionResult> DownloadFile(string fileId)
{
    var rsp = await client.GetObject("containerId", fileId);

    if (rsp.IsSuccess)
    {
        return new FileStreamResult(rsp.Stream, "application/octet-stream");
    }

    return new HttpNotFoundResult();
}
```

Chunked upload example for large files

```cs
public async Task<IActionResult> UploadChunk(int segment)
{
	if (Request.Form.Files != null && Request.Form.Files.Count > 0)
	{
		var file = Request.Form.Files[0];
		
		using (var fileStream = file.OpenReadStream())
		{
			using (var memoryStream = new MemoryStream())
			{
				var fileName = file.GetFileName();

				await fileStream.CopyToAsync(memoryStream);

				// upload file chunk
				await client.PutChunkedObject(containerTempId, fileName, memoryStream.ToArray(), segment);
			}
		}
	}

	return new JsonResult(new
	{
		Success = false
	});
}

public async Task<IActionResult> UploadDone(string fileName, string contentType)
{
	// use manifest to merge chunks
        await client.PutManifest(containerTempId, fileName);

        // copy chunks to new file and set some meta data info about the file (filename, contentype)
        await client.CopyObject(containerTempId, fileName, containerDemoId, fileName, new Dictionary<string, string>
        {
            { $"X-Object-Meta-{metaFileName}", fileName },
            { $"X-Object-Meta-{metaContentType}", contentType }
        });

        // cleanup temp
        await client.DeleteContainerContents(containerTempId);
}
```

Buffered download example using `BufferedHTTPStream`

```cs
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
```
