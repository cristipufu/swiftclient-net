# OpenStack Swift client for .NET 4.5 and 5.0

[![Build Status](https://img.shields.io/teamcity/http/tcoss.veritech.io/e/SwiftClient_Ubuntu.svg?label=Ubuntu build)]
(http://tcoss.veritech.io/viewType.html?buildTypeId=SwiftClient_Ubuntu&guest=1)
[![Build Status](https://img.shields.io/teamcity/http/tcoss.veritech.io/e/SwiftClient_Windows.svg?label=Windows build)]
(http://tcoss.veritech.io/viewType.html?buildTypeId=SwiftClient_Windows&guest=1)
[![NuGet version](https://img.shields.io/nuget/vpre/SwiftClient.svg)](https://www.nuget.org/packages/SwiftClient/)

SwiftClient is a HTTP wrapper over OpenStack Swift REST API and follows the [Object Storage API Reference](http://developer.openstack.org/api-ref-objectstorage-v1.html). It can be installed via NuGet from [nuget.org/packages/SwiftClient](https://www.nuget.org/packages/SwiftClient/) and it's compatible with .NET 4.5.1 and 5.0.

### Running the ASP.NET 5 MVC demo

The [SwiftClient.Demo](https://github.com/vtfuture/SwiftClient/tree/master/src/SwiftClient.Demo) project is an example of how to authenticate against swift, do chunked upload for a mp4 file and playing it using the HTML5 `video` tag. 

You will need at least one Ubuntu 14.04 box to host OpenStack Swfit proxy and storage. For dev/test environments we provide a docker image with a single swift proxy and storage, follow the setup instruction from [docker-swift](https://github.com/vtfuture/SwiftClient/tree/master/docker-swift) to build and run the swift container. After you've started the swift all-in-one container, put your Ubuntu box IP in the `appsettings.json` from the demo project and your good to go.

If you want to setup Swift for production on a Ubuntu cluster check out the [documentation](https://github.com/vtfuture/SwiftClient/wiki) from our wiki.

### Usage

The client implements a configurable retry mechanism, so you don't have to worry about the token expiration date or a temporary request failure. 
It also supports multiple endpoints (swfit proxy addreses), if you chose to use some of your proxy nodes for backup and not load balance exclude those from the endpoint list.

```cs
var swiftClient = new SwiftClient()
.WithCredentials(new SwiftCredentials
{
     Username = "system:root",
     Password = "testpass",
     Endpoints = new List<string> { 
		"http://192.168.3.31:8080",
		"http://192.168.3.32:8080"
		}
})
.SetRetryCount(4)
.SetRetryPerEndpointCount(2);
```

You have to supply your own implementation of `SwiftAuthManager` class and provide a caching mechanism for the ***authentication token*** so that each swift request is not being preceded by an authentication request. It is recommended to use a dedicated cache storage like Redis so multiple instances of your app can reuse the authentication token.

If you want to log swift failure events, just pass the client your implementation of the `ISwiftLogger` interface. Below is a stdout example:

```cs
public class SwiftLogger : ISwiftLogger
{
	private string _authError = "Exception occured: {0} for credentials {1} : {2} on proxy node {3}";
	private string _requestError = "Exception occured: {0} with status code: {1} for request url: {2}";
	private string _unauthorizedError = "Unauthorized request with old token {0}";

	public SwiftLogger()
	{
		var sw = new StreamWriter(Console.OpenStandardOutput());
		sw.AutoFlush = true;
		Console.SetOut(sw);
	}

	public void LogAuthenticationError(Exception e, string username, string password, string endpoint)
	{
		Console.Out.WriteLine(string.Format(_authError, e.Message, username, password, endpoint));
	}

	public void LogRequestError(WebException e, HttpStatusCode statusCode, string reason, string requestUrl)
	{
		Console.Out.WriteLine(string.Format(_requestError, reason, statusCode.ToString(), requestUrl));
	}

	public void LogUnauthorizedError(string token, string endpoint)
	{
		Console.Out.WriteLine(string.Format(_unauthorizedError, token));
	}
}
```

Set SwiftClient log:

```cs
var swiftClient = new SwiftClient()
.WithCredentials(new SwiftCredentials
{
     Username = "system:root",
     Password = "testpass",
     Endpoints = new List<string> { 
		"http://192.168.3.31:8080",
		"http://192.168.3.32:8080"
		}
})
.SetRetryCount(4)
.SetRetryPerEndpointCount(2)
.SetLogger(new SwiftLogger());
```

### ASP.NET 5 usage

You can load swift credentials from an json file in aspnet5 project. Add an app settings file to your project:

***appsettings.json***

```json
{
  "Credentials": {
    "Username": "test:tester",
    "Password": "testing",
    "Endpoints": [
      "https://192.168.3.31:8080",
      "https://192.168.3.32:8080"
    ]
  }
}
```
Load the settings at startup:

***Startup.cs***

```cs
public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
{
	// Setup configuration sources.
	var builder = new ConfigurationBuilder()
		.SetBasePath(appEnv.ApplicationBasePath)
		.AddJsonFile("appsettings.json")
		.AddEnvironmentVariables();
	Configuration = builder.Build();
}

public IConfigurationRoot Configuration { get; set; }

public void ConfigureServices(IServiceCollection services)
{
	// Add MVC services to the services container.
	services.AddMvc();

	services.Configure<SwiftCredentials>(Configuration.GetSection("Credentials"));
}
```

Use swift credentials in a controller:

***HomeController.cs***

```cs
public class HomeController : Controller
{
        SwiftClient client = new SwiftClient();

        public HomeController(IOptions<SwiftCredentials> credentials)
        {
            client.WithCredentials(credentials.Value)
                  .SetRetryCount(2)
                  .SetLogger(new SwiftLogger());

        }
}
```

# Achieve high availability and scalability with OpenStack Swift and SwiftClient

OpenStack Swift is an eventually consistent storage system designed to scale horizontally without any single point of failure. All objects are stored with multiple copies and are replicated across zones and regions making Swift  withstand failures in storage and network hardware. Swift can be used as a stand-alone distributed storage system on top of Linux without the need of expensive hardware solutions like NAS or SAN. 
Because data is stored and served directly over HTTP makes Swift the ideal solution when dealing with applications running in Docker containers.

Lets assume you have an ASP.NET 5 MVC app that needs to manage documents, photos and video files uploaded by users. To achieve HA and scalability of your application you can host the app inside a container and lunch a minimum of two container with a load balancer in front. Now days this can be easily done with Docker and Nginx on Ubuntu Server. The same architecture can be applied to the storage with Swift, you'll need to set-up a minimum of two swift server each containing a proxy and a storage node, ideally these servers or VMs should be hosted in a different region/datacenter. Adding both swift proxy endpoints to SwfitClient config will ensure that any app instance will share the same storage and if a swift node becomes unreachable due to a restart or network failure all app instances will silently fail-over to the 2ed node.

Below is a schematic view of our HA setup:

![Cluster](https://github.com/vtfuture/SwiftClient/blob/master/src/SwiftClient.Demo/wwwroot//img/app-cluster.png)


 
