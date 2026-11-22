#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TraduSaga.MVVM.Common
{
    public static class SafeExternalUrlPolicy
    {
        private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "github.com",
            "policies.google.com",
            "www.deepl.com",
            "yandex.com"
        };

        public static bool IsAllowed(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && uri.IsDefaultPort
                && string.IsNullOrEmpty(uri.UserInfo)
                && string.IsNullOrEmpty(uri.Fragment)
                && AllowedHosts.Contains(uri.IdnHost);
        }

        public static void Open(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !IsAllowed(value))
            {
                throw new InvalidOperationException("The requested link is not allowed.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
    }
}
