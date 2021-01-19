
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NETAPI.Models;

namespace NETAPI.Configuration
{
    public interface IApiConfiguration<TEnvironment, TEndpoint>
        where TEnvironment : Enum
        where TEndpoint : Enum
    {
        public int GlobalTimeoutSeconds { get; set; }
        public int MaxRequestAttempts { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public int DefaultMaxEndpointRequests { get; set; }

        public IDictionary<TEnvironment, string> EnvironmentURLs { get; set; }
        public ConcurrentDictionary<TEndpoint, Dictionary<string, string>> EndpointHeaders { get; set; }
        public ConcurrentDictionary<TEndpoint, int> MaxEndpointRequests { get; set; }
        public ConcurrentDictionary<TEndpoint, RequestDebounce> EndpointRequestDelays { get; set; }
        public ConcurrentDictionary<TEndpoint, int> ActiveRequests { get; set; }

        public TEnvironment CurrentEnvironment { get; set; }

        public string? OAuthBearerToken { get; set; }

        /// <summary>
        /// Configure this instance. All initialization should be added here.
        /// </summary>
        public void Configure();

        /// <summary>
        /// Add an API environment.
        /// </summary>
        /// <param name="env">The API environment to add.</param>
        /// <param name="baseUrl">The base url of the API environment.</param>
        public void AddEnvironment(TEnvironment env, string baseUrl);

        /// <summary>
        /// Set the currently selected API environment
        /// </summary>
        /// <param name="env">The API environment to set as current.</param>
        public void SetCurrentEnvironment(TEnvironment env);

        /// <summary>
        /// Set the global maximum number of concurrent requests.
        /// </summary>
        /// <param name="value">The maximum allowed concurrent requests.</param>
        public void SetMaxConcurrentRequests(int value);

        /// <summary>
        /// Set the maximum number of concurrent requests for the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set max concurrent requests for.</param>
        /// <param name="value">The maximum allowed concurrent requests.</param>
        public void SetMaxConcurrentRequests(TEndpoint endpoint, int value);

        /// <summary>
        /// Set the global minimum interval between making requests.
        /// </summary>
        /// <param name="delayMillis">The minimum interval between requests.</param>
        /// <param name="lastRequestTime">The last time this endpoint was requested.</param>
        public void SetMinimumInterval(
            int delayMillis,
            DateTime lastRequestTime);

        /// <summary>
        /// Set the minimum interval between making requests to the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the minimum request interval for.</param>
        /// <param name="delayMillis">The minimum interval between requests.</param>
        /// <param name="lastRequestTime">The last time this endpoint was requested.</param>
        public void SetMinimumInterval(
            TEndpoint endpoint,
            int delayMillis,
            DateTime lastRequestTime);

        /// <summary>
        /// Add a global header that applies to all requests for all endpoints.
        /// </summary>
        /// <param name="name">The name of header parameter.</param>
        /// <param name="value">The value of the header parameter.</param>
        public void AddHeader(
            string name,
            string value);

        /// <summary>
        /// Add a header to all requests to the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to add a header for.</param>
        /// <param name="name">The name of header parameter.</param>
        /// <param name="value">The value of the header parameter.</param>
        public void AddHeader(
            TEndpoint endpoint,
            string name,
            string value);
    }
}