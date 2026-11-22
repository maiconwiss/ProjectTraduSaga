using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TraduSaga.Utils.Http;

namespace TraduSaga.Security.Tests;

public class HttpReaderTests
{
    [Theory]
    [InlineData("http://translate.googleapis.com/translate_a/single")]
    [InlineData("https://translate.googleapis.com.attacker.invalid/translate_a/single")]
    [InlineData("https://translate.googleapis.com:444/translate_a/single")]
    [InlineData("https://user@translate.googleapis.com/")]
    public async Task RequestWebDataAsync_RejectsDestinationsOutsideTheExactHttpsAllowlist(string url)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var reader = new HttpReader(handler) { ThrowExceptions = false };

        var response = await reader.RequestWebDataAsync(url, HttpMethods.GET);

        Assert.False(response.IsSuccessful);
        Assert.Null(response.Body);
        Assert.Equal(0, handler.RequestCount);
        Assert.DoesNotContain(url, response.InnerException?.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://translate.googleapis.com/translate_a/single")]
    [InlineData("https://clients5.google.com/translate_a/t")]
    [InlineData("https://api-free.deepl.com/v2/translate")]
    [InlineData("https://api.deepl.com/v2/translate")]
    [InlineData("https://translate.api.cloud.yandex.net/translate/v2/translate")]
    public async Task RequestWebDataAsync_AcceptsEachAllowlistedHttpsHost(string url)
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        var reader = new HttpReader(handler);

        var response = await reader.RequestWebDataAsync(url, HttpMethods.GET);

        Assert.True(response.IsSuccessful);
        Assert.Equal("[]", response.Body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RequestWebDataAsync_DoesNotFollowRedirectsOrReturnErrorBody()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Content = new StringContent("private OCR text"),
                Headers = { Location = new Uri("https://attacker.invalid/collect") }
            });
        var reader = new HttpReader(handler) { ThrowExceptions = false };

        var response = await reader.RequestWebDataAsync("https://translate.googleapis.com/translate_a/single", HttpMethods.GET);

        Assert.False(response.IsSuccessful);
        Assert.Null(response.Body);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RequestWebDataAsync_EnforcesTheOneMebibyteResponseLimit()
    {
        var maxSizedReader = new HttpReader(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(new string('x', HttpReader.MaximumResponseBytes))
            }));

        var maxSizedResponse = await maxSizedReader.RequestWebDataAsync(
            "https://translate.googleapis.com/translate_a/single",
            HttpMethods.GET);
        Assert.True(maxSizedResponse.IsSuccessful);
        Assert.Equal(HttpReader.MaximumResponseBytes, System.Text.Encoding.UTF8.GetByteCount(maxSizedResponse.Body!));

        var oversizedReader = new HttpReader(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(new string('x', HttpReader.MaximumResponseBytes + 1))
            })) { ThrowExceptions = false };

        var oversizedResponse = await oversizedReader.RequestWebDataAsync(
            "https://translate.googleapis.com/translate_a/single",
            HttpMethods.GET);
        Assert.False(oversizedResponse.IsSuccessful);
        Assert.Null(oversizedResponse.Body);
        Assert.DoesNotContain(new string('x', 64), oversizedResponse.InnerException?.Message ?? string.Empty);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;
        private int _requestCount;

        public int RequestCount => _requestCount;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            return Task.FromResult(_responseFactory(request));
        }
    }
}
