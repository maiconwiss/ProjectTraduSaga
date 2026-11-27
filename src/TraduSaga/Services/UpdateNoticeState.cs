#nullable enable
using System;

namespace TraduSaga.Services
{
    internal sealed class UpdateNoticeState
    {
        private readonly Version _installedVersion;
        private Version? _availableVersion;

        internal UpdateNoticeState(Version installedVersion)
        {
            _installedVersion = installedVersion;
        }

        internal bool IsVisible => _availableVersion != null;

        internal string GetText(string format)
        {
            return _availableVersion == null
                ? string.Empty
                : string.Format(format, _installedVersion, _availableVersion);
        }

        internal void Show(Version availableVersion)
        {
            _availableVersion = availableVersion;
        }

        internal void Dismiss()
        {
            _availableVersion = null;
        }
    }
}
