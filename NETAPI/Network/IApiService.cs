using System;
using System.Threading.Tasks;
using NETAPI.Configuration;
using NETAPI.Models;

namespace NETAPI.Services
{
    public interface IApiService<TEnvironment, TEndpoint>
        where TEnvironment : Enum
        where TEndpoint : Enum
    {
        bool IsNetworkAvailable { get; }

        IApiConfiguration<TEnvironment, TEndpoint> Configuration { get; set; }

        /// <summary>
        /// Get the specified <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">The API endpoint to call.</param>
        /// <typeparam name="X">The type of response model.</typeparam>
        /// <exception cref="TooManyRequestsException">Thrown if too many requests are being made globally or to the given endpoint.</exception>
        /// <exception cref="TimeoutException">Thrown if a request times out after the configured interval.</exception>
        /// <returns>The response model populated with data from the specified endpoint.</returns>
        Task<ResponseBase<X?>?> Get<X>(TEndpoint endpoint);

        /// <summary>
        /// Gets the specified <paramref name="endpoint"/>, catching TimeoutException and TooManyRequestException.
        /// </summary>
        /// <param name="endpoint">The API endpoint to call.</param>
        /// <typeparam name="X">The type of response model.</typeparam>
        /// <returns>The response model populated with data from the specified endpoint.</returns>
        Task<ResponseBase<X?>?> TryGet<X>(TEndpoint endpoint);

        /// <summary>
        /// Posts the given <paramref name="data"/> to the given <paramref name="endpoint"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="data"></param>
        /// <exception cref="TooManyRequestsException">Thrown if too many requests are being made globally or to the given endpoint.</exception>
        /// <exception cref="TimeoutException">Thrown if a request times out after the configured interval.</exception>
        /// <returns>The response model populated with data from the specified endpoint.</returns>
        Task<ResponseBase<X?>?> Post<T, X>(
            TEndpoint endpoint,
            T data);


        /// <summary>
        /// Posts the given <paramref name="data"/> to the given <paramref name="endpoint"/>,
        /// catching TimeoutException and TooManyRequestException.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="data"></param>
        /// <returns>The response model populated with data from the specified endpoint.</returns>
        Task<ResponseBase<X?>?> TryPost<T, X>(
            TEndpoint endpoint,
            T data);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="directoryPath"></param>
        /// <param name="checkExisting"></param>
        /// <exception cref="TooManyRequestsException">Thrown if too many requests are being made globally or to the given endpoint.</exception>
        /// <exception cref="TimeoutException">Thrown if a request times out after the configured interval.</exception>
        /// <returns>The response model populated with a path to the downloaded file.</returns>
        Task<ResponseBase<string?>?> Download(
            TEndpoint endpoint,
            string filePath,
            string directoryPath = "NETAPI",
            bool checkExisting = true);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="filePath"></param>
        /// <param name="formData"></param>
        /// <exception cref="TooManyRequestsException">Thrown if too many requests are being made globally or to the given endpoint.</exception>
        /// <exception cref="TimeoutException">Thrown if a request times out after the configured interval.</exception>
        /// <returns></returns>
        Task<ResponseBase<object?>?> Upload(
            TEndpoint endpoint,
            string filePath,
            object formData);
    }
}
