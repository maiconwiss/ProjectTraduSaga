#nullable enable

using System;

namespace TraduSaga.Processing.TextProcessing
{
    internal enum ProgressiveTextCandidateStatus
    {
        Pending,
        Confirmed,
        Ignored
    }

    internal readonly record struct ProgressiveTextCandidateUpdate(
        ProgressiveTextCandidateStatus Status,
        string? Text);

    internal sealed class ProgressiveTextCandidate
    {
        private static readonly TimeSpan MaximumWait = TimeSpan.FromSeconds(10);

        private string? _pendingText;
        private string? _confirmedText;
        private TimeSpan _firstObservedAt;

        public bool HasPending => _pendingText != null;

        public string? PendingText => _pendingText;

        public bool HasConfirmedText => _confirmedText != null;

        public bool IsPrefixOfConfirmed(string text) =>
            _confirmedText != null
            && text.Length < _confirmedText.Length
            && _confirmedText.StartsWith(text, StringComparison.OrdinalIgnoreCase);

        public ProgressiveTextCandidateUpdate Observe(string text, TimeSpan elapsed)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return _pendingText != null && elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Ignored, null);
            }

            if (_pendingText == null)
            {
                if (string.Equals(_confirmedText, text, StringComparison.OrdinalIgnoreCase)
                    || IsPrefixOfConfirmed(text))
                {
                    return new(ProgressiveTextCandidateStatus.Ignored, null);
                }

                _pendingText = text;
                _firstObservedAt = elapsed;
                return new(ProgressiveTextCandidateStatus.Pending, _pendingText);
            }

            if (string.Equals(_pendingText, text, StringComparison.OrdinalIgnoreCase))
            {
                return ConfirmPending();
            }

            if (_pendingText.Length < text.Length
                && text.StartsWith(_pendingText, StringComparison.OrdinalIgnoreCase))
            {
                _pendingText = text;
                return elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Pending, _pendingText);
            }

            if (_pendingText.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                return elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Pending, _pendingText);
            }

            _confirmedText = _pendingText;
            _pendingText = text;
            _firstObservedAt = elapsed;
            return new(ProgressiveTextCandidateStatus.Confirmed, _confirmedText);
        }

        public void UpdatePendingWithBestText(string text)
        {
            if (_pendingText != null
                && _pendingText.Length < text.Length
                && text.StartsWith(_pendingText, StringComparison.OrdinalIgnoreCase))
            {
                _pendingText = text;
            }
        }

        public void Reset()
        {
            _pendingText = null;
            _confirmedText = null;
        }

        public void DiscardPending()
        {
            _pendingText = null;
        }

        private ProgressiveTextCandidateUpdate ConfirmPending()
        {
            _confirmedText = _pendingText;
            _pendingText = null;
            return new(ProgressiveTextCandidateStatus.Confirmed, _confirmedText);
        }
    }
}
