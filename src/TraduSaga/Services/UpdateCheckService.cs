#nullable enable
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TraduSaga.Services
{
    internal sealed class UpdateCheckService
    {
        internal const string ReleasesPageUrl = "https://github.com/maiconwiss/TraduSaga/releases";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/maiconwiss/TraduSaga/releases/latest";
        private readonly HttpClient _httpClient;

        public UpdateCheckService()
            : this(CreateHttpClient())
        {
        }

        internal UpdateCheckService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        internal async Task<Version?> CheckForUpdateAsync(Version installedVersion, CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
                request.Headers.UserAgent.ParseAdd("TraduSaga-UpdateChecker/1.0");
                request.Headers.Accept.ParseAdd("application/vnd.github+json");

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
                var root = document.RootElement;

                if (!root.TryGetProperty("draft", out var draft) || draft.GetBoolean()
                    || !root.TryGetProperty("prerelease", out var prerelease) || prerelease.GetBoolean()
                    || !root.TryGetProperty("tag_name", out var tagName)
                    || !TryParseVersion(tagName.GetString(), out var latestVersion))
                {
                    return null;
                }

                return latestVersion > installedVersion ? latestVersion : null;
            }
            catch (Exception exception) when (exception is HttpRequestException
                or TaskCanceledException
                or JsonException
                or InvalidOperationException
                or ArgumentException)
            {
                return null;
            }
        }

        internal static bool TryParseVersion(string? tagName, out Version version)
        {
            version = new Version(0, 0);
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

            var value = tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tagName.Substring(1) : tagName;
            if (value.Contains('-', StringComparison.Ordinal)
                || value.Contains('+', StringComparison.Ordinal)
                || !Version.TryParse(value, out var parsedVersion)
                || parsedVersion is null)
            {
                return false;
            }

            version = parsedVersion;
            return true;
        }

        internal static void OpenReleasesPage(Action<string> openUrl)
        {
            openUrl(ReleasesPageUrl);
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }
    }
}
