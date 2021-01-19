using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using NETAPI.Configuration;
using NETAPI.Exceptions;
using NETAPI.Extensions;
using NETAPI.Models;
using NETAPI.Utilities;

namespace NETAPI.Services
{
    public class ApiService<TEnvironment, TEndpoint> : IApiService<TEnvironment, TEndpoint>
        where TEnvironment : Enum
        where TEndpoint : Enum
    {

        private readonly Lazy<NetworkChecker> _networkChecker
            = new Lazy<NetworkChecker>(() => new NetworkChecker());

        public bool IsNetworkAvailable => _networkChecker.Value.HasInternet();

        public IApiConfiguration<TEnvironment, TEndpoint> Configuration { get; set; }

        public ApiService(IApiConfiguration<TEnvironment, TEndpoint> apiConfig)
        {
            Configuration = apiConfig;

            FlurlHttp.Configure(globals =>
                globals.Timeout = TimeSpan.FromSeconds(Configuration.GlobalTimeoutSeconds));
        }

        /// <summary>
        /// Makes the URL for the specified endpoint.
        /// </summary>
        /// <returns>The URL.</returns>
        /// <param name="endpoint">The API endpoint to call.</param>
        private IFlurlRequest GetRequest(TEndpoint endpoint)
        {
            if (Configuration.EnvironmentURLs == null) {
                throw new InvalidOperationException("No environments set for NETAPI. Did you configure `EnvironmentURLs`?");
            }
            if (Configuration.CurrentEnvironment == null) {
                throw new InvalidOperationException("No current environment set for NETAPI. Did you configure `CurrentEnvironment`?");
            }

            return Configuration.EnvironmentURLs[Configuration.CurrentEnvironment]
                .AppendPathSegment(endpoint?.ToString()?.ToLower())
                .WithHeaders(Configuration.EndpointHeaders[endpoint]);
        }

        /// <inherirdoc />
        public Task<ResponseBase<X?>?> Get<X>(TEndpoint endpoint) =>
            Request(endpoint, () => GetRequest(endpoint)
                .GetJsonAsync<ResponseBase<X?>?>());

        /// <inheritdoc />
        public Task<ResponseBase<X?>?> TryGet<X>(TEndpoint endpoint) =>
            TryRequest(() => Get<X>(endpoint));

        /// <inheritdoc />
        public Task<ResponseBase<X?>?> Post<T, X>(TEndpoint endpoint, T data) =>
            Request(endpoint, () => GetRequest(endpoint)
                .PostUrlEncodedAsync(data)
                .ReceiveJson<ResponseBase<X?>?>());

        /// <inheritdoc />
        public Task<ResponseBase<X?>?> TryPost<T, X>(TEndpoint endpoint, T data) =>
            TryRequest(() => Post<T, X>(endpoint, data));

        /// <inheritdoc />
        public Task<ResponseBase<object?>?> Upload(
            TEndpoint endpoint,
            string filePath,
            object formData) =>
            Request(endpoint, new Func<Task<ResponseBase<object?>?>>(async () => {
                var response = await GetRequest(endpoint)
                        .PostMultipartAsync(mp => mp
                            .AddStringParts(formData)
                            .AddFile("photo", File.OpenRead(filePath), Path.GetFileName(filePath)));

                response?.ResponseMessage?.EnsureSuccessStatusCode();

                return new ResponseBase<object?>() {
                    Success = response != null
                };
            }));

        /// <inheritdoc />
        public Task<ResponseBase<string?>?> Download(
            TEndpoint endpoint,
            string path,
            string directoryPath = "",
            bool checkExisting = true) =>
            Request(endpoint, async () => {

                var fileName = path
                    .Split('/')
                    .Last()
                    .ToValidFileName();

                var appFolder = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

                var folderPath = Path.Combine(appFolder, directoryPath);
                var filePath = Path.Combine(appFolder, directoryPath, fileName);

                if (checkExisting && File.Exists(filePath)) {
                    return ResultFromPath(filePath);
                }

                var result = await GetRequest(endpoint)
                    .DownloadFileAsync(folderPath, fileName);

                return ResultFromPath(result);

                static ResponseBase<string?>? ResultFromPath(string path) =>
                    new ResponseBase<string?> {
                        Success = true,
                        Data = path
                    };
            });

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="requestExecutor"></param>
        /// <returns></returns>
        private async Task<ResponseBase<T?>?> Request<T>(
            TEndpoint endpoint,
            Func<Task<ResponseBase<T?>?>> requestExecutor)
        {
            var tryCount = 0;

            while (tryCount < Configuration.MaxRequestAttempts) {
                Debug.WriteLine($"--- Requesting {endpoint}, attempt: {tryCount + 1}, already running: {(Configuration.ActiveRequests.TryGetValue(endpoint, out int running) ? running : 0)}");

                var timer = Stopwatch.StartNew();

                if (!ShouldExecuteRequest(endpoint)) {
                    throw new TooManyRequestsException();
                }

                if (ShouldDelayRequest(endpoint)) {
                    Debug.WriteLine($"--- Delaying {endpoint}");

                    await Task.Delay(Configuration
                        .EndpointRequestDelays[endpoint]
                        .RequestDelayMillis);

                    // After delaying, recheck if there are too many requests running
                    if (!ShouldExecuteRequest(endpoint)) {
                        throw new TooManyRequestsException();
                    }
                }

                Configuration.ActiveRequests.AddOrUpdate(
                    endpoint,
                    (_) => 1,
                    (endpoint, current) => current + 1);

                Configuration.EndpointRequestDelays.AddOrUpdate(
                    endpoint,
                    (_) => new RequestDebounce(0),
                    (endpoint, delayData) => {
                        delayData.LastRequestTime = DateTime.Now;
                        return delayData;
                    });

                try {
                    var response = await requestExecutor();

                    timer.Stop();

                    Debug.WriteLine($"--- Requesting {endpoint} complete after {tryCount + 1} attempt(s), took {timer.Elapsed}");

                    Configuration.ActiveRequests[endpoint]--;

                    return response;
                } catch (Exception e) {
                    timer.Stop();

                    Configuration.ActiveRequests[endpoint]--;

                    if (ShouldRetry(e,
                        endpoint.ToString(),
                        timer.Elapsed.ToString(),
                        ref tryCount)) {
                        await Task.Delay(1000 * (tryCount ^ 2));
                    } else if (e is TaskCanceledException
                        || e is FlurlHttpTimeoutException
                        || e.InnerException is TaskCanceledException) {
                        throw new TimeoutException();
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="requestExecutor"></param>
        /// <returns></returns>
        private async Task<ResponseBase<X?>?> TryRequest<X>(
            Func<Task<ResponseBase<X?>?>> requestExecutor)
        {
            ResponseBase<X?>? response = null;
            try {
                response = await requestExecutor();
            } catch (TooManyRequestsException e) {
                (response ??= new ResponseBase<X?>()).Exception = e;
                //_analyticsService.TrackException(e,
                //    "requesting",
                //    typeof(X).AssemblyQualifiedName);
            } catch (TimeoutException e) {
                (response ??= new ResponseBase<X?>()).Exception = e;
                //_analyticsService.TrackException(e,
                //    "requesting",
                //    typeof(X).AssemblyQualifiedName);
            }
            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private bool ShouldExecuteRequest(TEndpoint endpoint)
        {
            if (!Configuration.ActiveRequests.TryGetValue(endpoint, out int activeRequests)) {
                activeRequests = 0;
            }
            if (!Configuration.MaxEndpointRequests.TryGetValue(endpoint, out int max)) {
                max = Configuration.DefaultMaxEndpointRequests;
            }

            return activeRequests < max
                && Configuration.ActiveRequests.Values.Sum() < Configuration.MaxConcurrentRequests;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private bool ShouldDelayRequest(TEndpoint endpoint)
        {
            if (!Configuration.EndpointRequestDelays.TryGetValue(endpoint, out var delayData)) {
                delayData = new RequestDebounce(0);
            }

            return delayData
                .LastRequestTime
                .AddMilliseconds(delayData.RequestDelayMillis) > DateTime.Now;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="endpoint"></param>
        /// <param name="elapsedTime"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        private bool ShouldRetry(
            Exception e,
            string endpoint,
            string elapsedTime,
            ref int retryCount)
        {
            Debug.WriteLine($"--- API ERROR {endpoint}");
            Debug.WriteLine($"--- Attempt: {retryCount + 1}");
            Debug.WriteLine($"--- Elapsed: {elapsedTime}");
            Debug.WriteLine(e);

            //_analyticsService.TrackException(e, new Dictionary<string, string> {
            //    { "Endpoint", endpoint.ToString() },
            //    { "Time", elapsedTime },
            //    { "RetryCount", retryCount.ToString() }
            //});

            return ++retryCount != Configuration.MaxRequestAttempts;
        }
    }
}

