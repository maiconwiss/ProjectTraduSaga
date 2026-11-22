using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TraduSaga.Utils.Http
{
    public class HttpReader
    {
        public const int MaximumResponseBytes = 1024 * 1024;

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);
        private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "translate.googleapis.com",
            "clients5.google.com",
            "api-free.deepl.com",
            "api.deepl.com",
            "translate.api.cloud.yandex.net"
        };

        private static readonly HttpClient SharedClient = CreateClient();

        private readonly HttpClient _httpClient;

        public bool ThrowExceptions { get; set; } = true;
        public IDictionary<string, string> OptionalHeaders { get; set; }
        public string? UserAgent { get; set; } = "TraduSaga";
        public string? Accept { get; set; } = "*/*";
        public string? ContentType { get; set; } = "application/x-www-form-urlencoded";

        public HttpReader() : this(SharedClient)
        {
        }

        public HttpReader(HttpMessageHandler handler) : this(new HttpClient(handler, disposeHandler: false))
        {
        }

        private HttpReader(HttpClient httpClient)
        {
            _httpClient = httpClient;
            OptionalHeaders = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public Task<HttpResponse> RequestWebDataAsync(string url, HttpMethods method)
        {
            return RequestWebDataAsync(url, method, null);
        }

        public async Task<HttpResponse> RequestWebDataAsync(string url, HttpMethods method, string? dataIn)
        {
            var result = await RequestWebDataInternalAsync(url, method, dataIn).ConfigureAwait(false);
            if (ThrowExceptions && result.InnerException != null)
            {
                throw result.InnerException;
            }

            return result;
        }

        public HttpResponse RequestWebData(string url, HttpMethods method)
        {
            return RequestWebData(url, method, null);
        }

        public HttpResponse RequestWebData(string url, HttpMethods method, string? dataIn)
        {
            var result = RequestWebDataInternalAsync(url, method, dataIn).GetAwaiter().GetResult();
            if (ThrowExceptions && result.InnerException != null)
            {
                throw result.InnerException;
            }

            return result;
        }

        private async Task<HttpResponse> RequestWebDataInternalAsync(string url, HttpMethods method, string? dataIn)
        {
            if (!TryGetAllowedUri(url, out var uri))
            {
                return CreateFailure("The request destination is not allowed.");
            }

            if (method != HttpMethods.GET && method != HttpMethods.POST)
            {
                return CreateFailure("The request method is not allowed.");
            }

            try
            {
                using var timeout = new CancellationTokenSource(RequestTimeout);
                using var request = new HttpRequestMessage(new HttpMethod(method.ToString()), uri);

                if (!TryAddHeaders(request))
                {
                    return CreateFailure("The request headers are invalid.");
                }

                if (dataIn != null)
                {
                    request.Content = new StringContent(dataIn, Encoding.UTF8);
                    if (ContentType != null)
                    {
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
                    }
                }

                using var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        timeout.Token)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new HttpResponse(
                        isSuccessful: false,
                        body: null,
                        exception: new HttpRequestException("The HTTP request failed."),
                        statusCode: response.StatusCode);
                }

                var body = await ReadResponseBodyAsync(response, timeout.Token).ConfigureAwait(false);
                return new HttpResponse(true, body, null, response.StatusCode);
            }
            catch (OperationCanceledException)
            {
                return CreateFailure("The HTTP request timed out.");
            }
            catch (HttpRequestException)
            {
                return CreateFailure("The HTTP request failed.");
            }
            catch (IOException)
            {
                return CreateFailure("The HTTP response could not be read.");
            }
            catch (InvalidDataException)
            {
                return CreateFailure("The HTTP response exceeded the size limit.");
            }
            catch (FormatException)
            {
                return CreateFailure("The HTTP request headers are invalid.");
            }
        }

        private bool TryAddHeaders(HttpRequestMessage request)
        {
            if (UserAgent != null && !request.Headers.TryAddWithoutValidation("User-Agent", UserAgent))
            {
                return false;
            }

            if (Accept != null && !request.Headers.TryAddWithoutValidation("Accept", Accept))
            {
                return false;
            }

            foreach (KeyValuePair<string, string> header in OptionalHeaders)
            {
                if (header.Value != null && !request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<string> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.Content.Headers.ContentLength > MaximumResponseBytes)
            {
                throw new InvalidDataException("Response body limit exceeded.");
            }

            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var readBuffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + bytesRead > MaximumResponseBytes)
                {
                    throw new InvalidDataException("Response body limit exceeded.");
                }

                buffer.Write(readBuffer, 0, bytesRead);
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private static bool TryGetAllowedUri(string url, out Uri uri)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) || parsedUri == null)
            {
                uri = null!;
                return false;
            }

            uri = parsedUri;
            return uri.Scheme == Uri.UriSchemeHttps
                && uri.IsDefaultPort
                && string.IsNullOrEmpty(uri.UserInfo)
                && string.IsNullOrEmpty(uri.Fragment)
                && AllowedHosts.Contains(uri.IdnHost);
        }

        private static HttpResponse CreateFailure(string message)
        {
            return new HttpResponse(false, null, new HttpRequestException(message));
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false,
                SslProtocols = SslProtocols.Tls12
            };

            return new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }
    }
}
