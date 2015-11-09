# OpenStack Swift client for .NET 4.5 and 5.0

[![Build Status](https://img.shields.io/teamcity/http/tcoss.veritech.io/e/SwiftClient_Ubuntu.svg?label=Ubuntu build)]
(http://tcoss.veritech.io/viewType.html?buildTypeId=SwiftClient_Ubuntu&guest=1)
[![Build Status](https://img.shields.io/teamcity/http/tcoss.veritech.io/e/SwiftClient_Windows.svg?label=Windows build)]
(http://tcoss.veritech.io/viewType.html?buildTypeId=SwiftClient_Windows&guest=1)
[![NuGet version](https://img.shields.io/nuget/vpre/SwiftClient.svg)](https://www.nuget.org/packages/SwiftClient/)

### Usage

SwiftClient is a HTTP wrapper over OpenStack Swift REST API and follows the [Object Storage API Reference](http://developer.openstack.org/api-ref-objectstorage-v1.html). It can be installed via NuGet from [nuget.org/packages/SwiftClient](https://www.nuget.org/packages/SwiftClient/) and it's compatible with .NET 4.5.1 and 5.0.

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

You have to implement the abstract class SwiftClientBase and provide a caching mechanism for the ***authentication token*** so that each swift request is not being preceded by an authentication request.
Below is a SwiftClient implementation that stores the authentication token in memory, it is recommended to use a dedicated cache storage like Redis so multiple instances of your app can reuse the authentication token.

```cs
public class SwiftClient : SwiftClientBase
{
	protected SwiftAuthData _authData;
	protected List<string> _endpoints;

	public SwiftClient() : base() { }

	public SwiftClient(SwiftCredentials credentials) : base(credentials) { }

	public SwiftClient(SwiftCredentials credentials, SwiftConfig config) : base(credentials, config) { }

	public SwiftClient(SwiftCredentials credentials, ISwiftLogger logger) : base(credentials, logger) { }

	public SwiftClient(SwiftCredentials credentials, SwiftConfig config, ISwiftLogger logger) : base(credentials, config, logger) { }

	/// <summary>
	/// Use for caching the authentication token
	/// If you don't cache the authentication token, each swift call will be preceded by an auth call 
	///     to obtain the token
	/// </summary>
	/// <param name="authData"></param>
	protected override void SetAuthData(SwiftAuthData authData)
	{
		_authData = authData;
	}

	/// <summary>
	/// Get authentication token from cache
	/// </summary>
	/// <returns></returns>
	protected override SwiftAuthData GetAuthData()
	{
		return _authData;
	}

	/// <summary>
	/// Get cached proxy endpoints (ordered by priority)
	/// If you don't cache the list, each swift call will try the proxy nodes in the initial priority order
	/// </summary>
	/// <returns></returns>
	protected override List<string> GetEndpoints()
	{
		return _endpoints ?? _credentials.Endpoints;
	}

	/// <summary>
	/// Save new endpoints order in cache
	/// </summary>
	/// <param name="endpoints"></param>
	protected override void SetEndpoints(List<string> endpoints)
	{
		_endpoints = endpoints;
	}
}
```

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

### Running the ASP.NET 5 MVC demo site

The [SwiftClient.Demo](https://github.com/vtfuture/SwiftClient/tree/master/src/SwiftClient.Demo) project is an example of how to authenticate against swift, do chunked upload for an mp4 file and playing it using the HTML5 `video` tag. For dev/test environments we provide a docker image with a single swift proxy and storage, follow the setup instruction from [docker-swift](https://github.com/vtfuture/SwiftClient/tree/master/docker-swift).
