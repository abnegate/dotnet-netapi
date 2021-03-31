# NETAPI
NETAPI provides an easy to use yet feature rich API client implementation for use in any C# project.

## Installation
NETAPI is available via [nuget](https://www.nuget.org/packages/NETAPI/)

`dotnet add package NETAPI`

## Usage
Given the following endpoint enum:
```csharp
    public enum Endpoint
    {
        Upload,
        Download,
        Login,
        FooModel,
        BarModel
    }
```

And the following environments:
```csharp
    public enum Environment
    {
        Dev,
        Test,
        Production
    }
```

Create an ApiConfiguration class:
```csharp
    public class ApiConfiguration : ApiConfigurationBase<Environment, Endpoint>
    {
        // Implement the abstract configure function
        public override void Configure()
        {
            // Add your envrionments
            AddEnvironment(Environment.Dev, "https://dev.api.com");
            AddEnvironment(Environment.Test, "https://uat.api.com");
            AddEnvironment(Environment.Production, "https://prod.api.com");

            // Set the current environment
            SetCurrentEnvironment(Environment.Dev);

            // Add headers for each endpoint
            AddHeader("Content-Type", "application/json");
            // Add headers per endpoint
            AddHeader(Endpoint.Login, "Content-Type", "application/json");

            // Set max concurrent requests for each endpoint
            SetMaxConcurrentRequests(2);
            // Set max concurrent requests per endpoint
            SetMaxConcurrentRequests(Endpoint.Upload, 5);
            SetMaxConcurrentRequests(Endpoint.Download, 5);
            SetMaxConcurrentRequests(Endpoint.Login, 1);

            // Set the minimum request interval for each endpoint
            SetMinimumInterval(5000);
            // Set the minimum request interval per endpoint
            SetMinimumInterval(Endpoint.Login, 5000);
        }
    }
```

Inject or instantiate an ApiService:
```csharp
    ...

    readonly IApiService<Environment, Endpoint> _apiService;

    public Action LoginCommand =>
        new Action(async () => await OnLoginAsync());

    public Action GetItemsCommand =>
        new Action(async () => await GetItemsAsync());

    // Directly constructing the service and configuration
    public Foo()
    {
        _apiService = new ApiService<Environment, Endpoint>(new ApiConfiguration());
    }

    // If you use a dependency injection or IOC, both IApiService and IApiConfiguration can be injected;
    //
    // With explicit registration:
    //     _container.Register<IApiService, ApiService>();
    //     _container.Register<IApiConfiguration<Environment, Endpoint>, ApiConfiguration>();
    public Foo(IApiService<Environment, Endpoint> apiService)
    {
        _apiService = apiService;
    }

    ...
```

Get models fom the API:
```csharp
    private async Task BarAsync()
    {
        ResponseBase<FooModel?>? response =
            await _apiService.Get<FooModel>(Endpoint.FooModel);

        if (response != null
            && response.Data != null
            && response.Success) {

            FooModel model = response.Data;

            Debug.WriteLine(model?.BarModel?.Bar);
        }
    }
```

## Authentication
If you need to authenticate with the API, make your auth call, then set service configuration `OAuthBearerToken` property.
```csharp
    private async Task LoginAsync()
    {
        ResponseBase<LoginResponse?>? response = await _apiService.Post<LoginRequest, LoginResponse>(
            Endpoint.Login,
            new LoginRequest("test@example.com", "password"));

        if (response != null
            && response.Data != null
            && response.Success) {

            _apiService
                .Configuration
                .OAuthBearerToken = response.Data.Token;
        }
    }
```

Setting `OAuthBearerToken` will automatically attach an "Authorization: Bearer {token}" header to every configured endpoint.

## Development
Functionallity can be manipulated by creating custom services and configurations.

To get started, first create a custom configuration:
```chsarp
    public class MyConfig: ApiConfigurationBase<Environment, Endpoint> { }
```

Then create a custom service:
```csharp
    public MyApiService: ApiService<MyEnvironments, MyEndpoints> { }
```

You can then override the default methods for any functions in IApiService.
