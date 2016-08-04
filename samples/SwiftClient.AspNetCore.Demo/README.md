# Getting Started with ASP.NET Core and OpenStack Swift

With ASP.NET Core being released .NET developers will switch to containers as the default deployment model, breaking away from IIS and monolithic apps is about to happen. 
Running ASP.NET web apps in containers being docker or windows containers will simplify and speed up not only deployment but also the build and test operations. 

Lets assume you have an ASP.NET Core MVC app that manages documents, photos and video files uploaded by users. 
Your application scales horizontally by running in multiple containers behind a reverse proxy. 
All your application instances must access the storage where user files are stored in order to function, but having the same storage shared between containers can a be a challenge.
If you have a HA setup then your containers are running on multiple machines, probability not event in the same data center, so instead of storing the files on disk your application could use a distributed storage system like OpenStack Swift.

OpenStack Swift is an eventually consistent storage system designed to scale horizontally without any single point of failure. All objects are stored with multiple copies and are replicated across zones and regions making Swift withstand failures in storage and network hardware. Swift can be used as a stand-alone distributed storage system on top of Linux without the need of expensive hardware solutions like NAS or SAN.
Data is stored and served directly over HTTP making Swift the ideal solution when dealing with applications running in containers.

Lets assume you've installed an OpenStack Swift cluster with a minimum of two swift servers, each containing a proxy and a storage node. Ideally these servers or VMs should be hosted in different regions/datacenters.
In order to access the Swift cluster from your ASP.NET Core application you can use SwiftClient.AspNetCore package. Adding both swift proxy endpoints to SwfitClient config will ensure that any app instance will share the same storage and if a Swift node becomes unreachable due to a restart or network failure all app instances will silently fail-over to the 2nd node.

Install NuGet package:

```
Install-Package SwiftClient.AspNetCore
```

Add Swift endpoints and credentials in `appsettings.json`:

```json
  "SwiftCluster": {
    "Username": "test:tester",
    "Password": "testing",
    "Endpoints": [
      "http://localhost:8080",
	  "http://localhost:8081",
    ],
    "RetryCount": 1,
    "RetryPerEndpointCount": 2
  }
```

Configure SwiftClient service in Startup.cs:

```cs
public void ConfigureServices(IServiceCollection services)
{
	services.AddOptions();
	services.AddMemoryCache();

	services.AddMvc();

	services.Configure<SwiftServiceOptions>(Configuration.GetSection("SwiftCluster"));
	services.AddSingleton<ISwiftLogger, SwiftServiceLogger>();
	services.AddSingleton<ISwiftAuthManager, SwiftAuthManagerMemoryCache>();
	services.AddTransient<ISwiftClient, SwiftService>();
}
```

Use the `SwiftService` in your controller:

```cs
private readonly ISwiftClient _swiftService;

public HomeController(ISwiftClient swiftService)
{
	_swiftService = swiftService;
}
```

Simple upload/download example for small files:

```cs
public async Task<IActionResult> UploadFile(IFormFile file)
{ 
    using (var fileStream = file.OpenReadStream())
    {
        using (var memoryStream = new MemoryStream())
        {
            await fileStream.CopyToAsync(memoryStream);

            var resp = await _swiftService.PutObject(containerId, fileId, memoryStream);

            return new JsonResult(new
            {
                Success = resp.IsSuccess
            });
        }
    }
}

public async Task<IActionResult> DownloadFile(string fileId)
{
    var rsp = await _swiftService.GetObject("containerId", fileId);

    if (rsp.IsSuccess)
    {
        return new FileStreamResult(rsp.Stream, "application/octet-stream");
    }

    return new NotFoundResult();
}
```

Chunked upload example for large files:

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
				await _swiftService.PutChunkedObject(containerTempId, fileName, memoryStream.ToArray(), segment);
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
        await _swiftService.PutManifest(containerTempId, fileName);

        // copy chunks to new file and set some meta data info about the file (filename, contentype)
        await _swiftService.CopyObject(containerTempId, fileName, containerDemoId, fileName, new Dictionary<string, string>
        {
            { $"X-Object-Meta-{metaFileName}", fileName },
            { $"X-Object-Meta-{metaContentType}", contentType }
        });

        // cleanup temp
        await _swiftService.DeleteContainerContents(containerTempId);
}
```

Buffered download example using `BufferedHTTPStream`:

```cs
public async Task<IActionResult> DownloadFile(string fileId)
{
	var headObject = await _swiftService.HeadObject(containerId, fileId);

	if (headObject.IsSuccess && headObject.ContentLength > 0)
	{
		var fileName = headObject.GetMeta("Filename");
		var contentType = headObject.GetMeta("Contenttype");

		Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

		var stream = new BufferedHTTPStream((start, end) =>
		{
			using (var response = _swiftService.GetObjectRange(containerId, objectId, start, end).Result)
           {
	                var ms = new MemoryStream();
	
	                response.Stream.CopyTo(ms);
	
	                return ms;
           }

		}, () => headObject.ContentLength);

		return new FileStreamResult(stream, contentType);
	}

	return new NotFoundResult();
}
```
