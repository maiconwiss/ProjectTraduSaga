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

    internal readonly record struct ProgressiveTextCandidateUpdate<T>(
        ProgressiveTextCandidateStatus Status,
        string? Text,
        T? Result);

    internal sealed class ProgressiveTextCandidate<T>
    {
        private static readonly TimeSpan MaximumWait = TimeSpan.FromSeconds(10);

        private string? _pendingText;
        private string? _confirmedText;
        private T? _pendingResult;
        private TimeSpan _firstObservedAt;

        public bool HasPending => _pendingText != null;

        public string? PendingText => _pendingText;

        public bool HasConfirmedText => _confirmedText != null;

        public TimeSpan? RemainingWait(TimeSpan elapsed)
        {
            return _pendingText == null
                ? null
                : MaximumWait - (elapsed - _firstObservedAt);
        }

        public bool IsPrefixOfConfirmed(string text) =>
            _confirmedText != null
            && text.Length < _confirmedText.Length
            && _confirmedText.StartsWith(text, StringComparison.OrdinalIgnoreCase);

        public ProgressiveTextCandidateUpdate<T> Observe(string text, T? result, TimeSpan elapsed)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return _pendingText != null && elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Ignored, null, default);
            }

            if (_pendingText == null)
            {
                if (string.Equals(_confirmedText, text, StringComparison.OrdinalIgnoreCase)
                    || IsPrefixOfConfirmed(text))
                {
                    return new(ProgressiveTextCandidateStatus.Ignored, null, default);
                }

                _pendingText = text;
                _pendingResult = result;
                _firstObservedAt = elapsed;
                return new(ProgressiveTextCandidateStatus.Pending, _pendingText, _pendingResult);
            }

            if (string.Equals(_pendingText, text, StringComparison.OrdinalIgnoreCase))
            {
                return ConfirmPending();
            }

            if (_pendingText.Length < text.Length
                && text.StartsWith(_pendingText, StringComparison.OrdinalIgnoreCase))
            {
                _pendingText = text;
                _pendingResult = result;
                return elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Pending, _pendingText, _pendingResult);
            }

            if (_pendingText.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                return elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Pending, _pendingText, _pendingResult);
            }

            var confirmedText = _pendingText;
            var confirmedResult = _pendingResult;
            _confirmedText = _pendingText;
            _pendingText = text;
            _pendingResult = result;
            _firstObservedAt = elapsed;
            return new(ProgressiveTextCandidateStatus.Confirmed, confirmedText, confirmedResult);
        }

        public void UpdatePendingWithBestText(string text, T result)
        {
            if (_pendingText == null)
            {
                return;
            }

            if (string.Equals(_pendingText, text, StringComparison.OrdinalIgnoreCase))
            {
                _pendingResult = result;
            }
            else if (_pendingText.Length < text.Length
                && text.StartsWith(_pendingText, StringComparison.OrdinalIgnoreCase))
            {
                _pendingText = text;
                _pendingResult = result;
            }
        }

        public void Reset()
        {
            _pendingText = null;
            _confirmedText = null;
            _pendingResult = default;
        }

        public void DiscardPending()
        {
            _pendingText = null;
            _pendingResult = default;
        }

        private ProgressiveTextCandidateUpdate<T> ConfirmPending()
        {
            _confirmedText = _pendingText;
            _pendingText = null;
            var result = _pendingResult;
            _pendingResult = default;
            return new(ProgressiveTextCandidateStatus.Confirmed, _confirmedText, result);
        }
    }
}
