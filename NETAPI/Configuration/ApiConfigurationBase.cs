using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NETAPI.Models;

namespace NETAPI.Configuration
{
    public abstract class ApiConfigurationBase<TEnvironment, TEndpoint> : IApiConfiguration<TEnvironment, TEndpoint>
        where TEnvironment : Enum
        where TEndpoint : Enum
    {
        public int GlobalTimeoutSeconds { get; set; } = 60;
        public int MaxRequestAttempts { get; set; } = 1;
        public int MaxConcurrentRequests { get; set; } = 10;
        public int DefaultMaxEndpointRequests { get; set; } = 2;

        public IDictionary<TEnvironment, string> EnvironmentURLs { get; set; }
            = new Dictionary<TEnvironment, string>();

        public ConcurrentDictionary<TEndpoint, Dictionary<string, string>> EndpointHeaders { get; set; }
            = new ConcurrentDictionary<TEndpoint, Dictionary<string, string>>();

        public ConcurrentDictionary<TEndpoint, int> MaxEndpointRequests { get; set; }
            = new ConcurrentDictionary<TEndpoint, int>();

        public ConcurrentDictionary<TEndpoint, RequestDebounce> EndpointRequestDelays { get; set; }
            = new ConcurrentDictionary<TEndpoint, RequestDebounce>();

        public ConcurrentDictionary<TEndpoint, int> ActiveRequests { get; set; }
            = new ConcurrentDictionary<TEndpoint, int>();

        public TEnvironment CurrentEnvironment { get; set; }

        private string? _bearerToken;
        public string? OAuthBearerToken
        {
            get => _bearerToken;
            set
            {
                _bearerToken = value;

                foreach (TEndpoint ep in Enum.GetValues(typeof(TEndpoint))) {
                    EndpointHeaders.AddOrUpdate(ep,
                        (_) => new Dictionary<string, string> { { "Authorization", $"Bearer {_bearerToken}" } },
                        (_, headers) => {
                            headers["Authorization"] = $"Bearer {_bearerToken}";
                            return headers;
                        });
                }
            }
        }

        public ApiConfigurationBase()
        {
            Configure();
        }

        ///<inheritdoc/>
        public abstract void Configure();

        ///<inheritdoc/>
        public void AddEnvironment(TEnvironment env, string baseUrl)
        {
            EnvironmentURLs.Add(env, baseUrl);
        }

        ///<inheritdoc/>
        public void SetCurrentEnvironment(TEnvironment env)
        {
            CurrentEnvironment = env;
        }

        ///<inheritdoc/>
        public void SetMaxConcurrentRequests(int value)
        {
            ForEachEndpoint(ep => SetMaxConcurrentRequests(ep, value));
        }

        ///<inheritdoc/>
        public void SetMaxConcurrentRequests(TEndpoint endpoint, int value)
        {
            MaxEndpointRequests.TryAdd(endpoint, value);
        }

        ///<inheritdoc/>
        public void SetMinimumInterval(
            int delayMillis,
            DateTime lastRequestTime = default)
        {
            ForEachEndpoint(ep => SetMinimumInterval(ep, delayMillis, lastRequestTime));
        }

        ///<inheritdoc/>
        public void SetMinimumInterval(
            TEndpoint endpoint,
            int delayMillis,
            DateTime lastRequestTime = default)
        {
            if (lastRequestTime == default) {
                lastRequestTime = DateTime.MinValue;
            }

            EndpointRequestDelays.AddOrUpdate(
                endpoint,
                new RequestDebounce(delayMillis, lastRequestTime),
                (_, delay) => {
                    delay.RequestDelayMillis = delayMillis;
                    delay.LastRequestTime = DateTime.Now;
                    return delay;
                });

        }

        ///<inheritdoc/>
        public void AddHeader(string name, string value)
        {
            ForEachEndpoint(ep => AddHeader(ep, name, value));
        }

        ///<inheritdoc/>
        public void AddHeader(TEndpoint endpoint, string name, string value)
        {
            EndpointHeaders.AddOrUpdate(
                endpoint,
                new Dictionary<string, string> { { name, value } },
                (_, headers) => {
                    headers[name] = value;
                    return headers;
                });
        }

        /// <summary>
        /// Loop each possible <typeparamref name="TEndpoint"/> and run the given action.
        /// </summary>
        /// <param name="action">To run with each endpoint.</param>
        private void ForEachEndpoint(Action<TEndpoint> action)
        {
            foreach (TEndpoint ep in Enum.GetValues(typeof(TEndpoint))) {
                action(ep);
            }
        }
    }
}
