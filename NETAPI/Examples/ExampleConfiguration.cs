using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NETAPI.Configuration;
using NETAPI.Models;
using NETAPI.Services;

namespace NETAPI.Examples
{
    // Given the following endpoints
    public enum Endpoint
    {
        Upload,
        Download,
        Login,
        FooModel,
        BarModel
    }

    // And the following environments
    public enum Environment
    {
        Dev,
        Test,
        Production
    }

    // Create an ApiConfiguration class
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


    // If there is a login request class defined
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public LoginRequest(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    // As well as a response class that can contain and OAuthToken
    public class LoginResponse
    {
        public string? Token { get; set; }
    }

    public class FooModel
    {
        public string? Foo { get; set; }
        public BarModel? BarModel { get; set; }
    }

    public class BarModel
    {
        public int Bar { get; set; }
    }


    // A view model can exist with an api dependency
    public class ApiInteractor
    {
        readonly IApiService<Environment, Endpoint> _apiService;

        public Action LoginCommand =>
            new Action(async () => await OnLoginAsync());

        public Action GetItemsCommand =>
            new Action(async () => await GetItemsAsync());

        // Directly constructing the service and configuration
        public ApiInteractor()
        {
            _apiService = new ApiService<Environment, Endpoint>(new ApiConfiguration());
        }

        // If you use a dependency injection or IOC, both IApiService and IApiConfiguration can be injected;
        //
        // With explicit registration:
        //     _container.Register<IApiService, ApiService>();
        //     _container.Register<IApiConfiguration<Environment, Endpoint>, ApiConfiguration>();
        public ApiInteractor(IApiService<Environment, Endpoint> apiService)
        {
            _apiService = apiService;
        }

        private async Task OnLoginAsync()
        {
            var response = await _apiService.Post<LoginRequest, LoginResponse>(
                Endpoint.Login,
                new LoginRequest("test@example.com", "password"));

            if (response != null
                && response.Data != null
                && response.Success) {

                // Setting `OAuthBearerToken` will automatically attach an "Authorization: Bearer {token}" header to every configured endpoint.
                _apiService
                    .Configuration
                    .OAuthBearerToken = response.Data.Token;
            }
        }

        private async Task GetItemsAsync()
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
    }
}
