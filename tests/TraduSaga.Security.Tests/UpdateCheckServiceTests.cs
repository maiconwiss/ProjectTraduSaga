using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TraduSaga.Services;

namespace TraduSaga.Security.Tests;

public sealed class UpdateCheckServiceTests
{
    [Theory]
    [InlineData("1.9.0", "v1.10.0", true)]
    [InlineData("1.10.0", "v1.9.0", false)]
    [InlineData("1.10.0", "1.10.0", false)]
    [InlineData("1.0.0", "v1.0.1", true)]
    public async Task CheckForUpdateAsync_ComparesNumericVersions(string installed, string latest, bool expectedUpdate)
    {
        using var client = CreateClient($"{{\"draft\":false,\"prerelease\":false,\"tag_name\":\"{latest}\"}}");
        var service = new UpdateCheckService(client);

        var result = await service.CheckForUpdateAsync(Version.Parse(installed));

        Assert.Equal(expectedUpdate, result is not null);
        if (expectedUpdate)
        {
            Assert.Equal(Version.Parse(latest.TrimStart('v')), result);
        }
    }

    [Theory]
    [InlineData("{\"draft\":true,\"prerelease\":false,\"tag_name\":\"v2.0.0\"}")]
    [InlineData("{\"draft\":false,\"prerelease\":true,\"tag_name\":\"v2.0.0\"}")]
    [InlineData("{\"draft\":false,\"prerelease\":false,\"tag_name\":\"v2.0.0-rc.1\"}")]
    [InlineData("invalid json")]
    public async Task CheckForUpdateAsync_IgnoresDraftPrereleaseAndInvalidPayload(string payload)
    {
        using var client = CreateClient(payload);
        var service = new UpdateCheckService(client);

        var result = await service.CheckForUpdateAsync(new Version(1, 0, 0));

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_IgnoresHttpErrorsAndNetworkFailures()
    {
        using var errorClient = CreateClient("", HttpStatusCode.Forbidden);
        using var failedClient = new HttpClient(new ThrowingHandler());

        Assert.Null(await new UpdateCheckService(errorClient).CheckForUpdateAsync(new Version(1, 0, 0)));
        Assert.Null(await new UpdateCheckService(failedClient).CheckForUpdateAsync(new Version(1, 0, 0)));
    }

    [Theory]
    [InlineData("v1.10.0", true)]
    [InlineData("1.10", true)]
    [InlineData("v1.10.0-beta", false)]
    [InlineData("not-a-version", false)]
    public void TryParseVersion_AcceptsStableNumericTagsOnly(string tag, bool expected)
    {
        Assert.Equal(expected, UpdateCheckService.TryParseVersion(tag, out _));
    }

    [Fact]
    public void OpenReleasesPage_UsesOfficialReleasesUrl()
    {
        string? openedUrl = null;

        UpdateCheckService.OpenReleasesPage(url => openedUrl = url);

        Assert.Equal("https://github.com/maiconwiss/TraduSaga/releases", openedUrl);
    }

    [Fact]
    public void UpdateNoticeState_ShowsVersionsAndCanBeDismissed()
    {
        var notice = new UpdateNoticeState(new Version(1, 9, 0));

        Assert.False(notice.IsVisible);
        Assert.Equal(string.Empty, notice.GetText("Available: {0} -> {1}"));

        notice.Show(new Version(1, 10, 0));

        Assert.True(notice.IsVisible);
        Assert.Equal("Available: 1.9.0 -> 1.10.0", notice.GetText("Available: {0} -> {1}"));

        notice.Dismiss();

        Assert.False(notice.IsVisible);
        Assert.Equal(string.Empty, notice.GetText("Available: {0} -> {1}"));
    }

    private static HttpClient CreateClient(string payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new StubHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Network unavailable");
        }
    }
}
