#nullable enable

using System;
using System.Buffers;

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
        private bool _pendingWasExtended;

        public bool HasPending => _pendingText != null;

        public string? PendingText => _pendingText;

        public bool HasConfirmedText => _confirmedText != null;

        public bool PendingWasExtended => _pendingWasExtended;

        public TimeSpan? RemainingWait(TimeSpan elapsed)
        {
            return _pendingText == null
                ? null
                : MaximumWait - (elapsed - _firstObservedAt);
        }

        public bool IsDuplicateOfConfirmed(string text) =>
            _confirmedText != null
            && (AreEquivalent(text, _confirmedText)
                || IsStrictPrefix(text, _confirmedText)
                || AreSimilar(text, _confirmedText));

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
                if (AreEquivalent(_confirmedText, text)
                    || IsDuplicateOfConfirmed(text))
                {
                    return new(ProgressiveTextCandidateStatus.Ignored, null, default);
                }

                _pendingText = text;
                _pendingResult = result;
                _firstObservedAt = elapsed;
                return new(ProgressiveTextCandidateStatus.Pending, _pendingText, _pendingResult);
            }

            if (AreEquivalent(_pendingText, text))
            {
                return ConfirmPending();
            }

            if (IsStrictPrefix(_pendingText, text))
            {
                _pendingText = text;
                _pendingResult = result;
                _pendingWasExtended = true;
                return elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Pending, _pendingText, _pendingResult);
            }

            if (IsStrictPrefix(text, _pendingText))
            {
                return elapsed - _firstObservedAt >= MaximumWait
                    ? ConfirmPending()
                    : new(ProgressiveTextCandidateStatus.Pending, _pendingText, _pendingResult);
            }

            if (AreSimilar(_pendingText, text))
            {
                _pendingText = text;
                _pendingResult = result;

                _pendingWasExtended = true;
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
            _pendingWasExtended = false;
            return new(ProgressiveTextCandidateStatus.Confirmed, confirmedText, confirmedResult);
        }

        public void UpdatePendingWithBestText(string text, T result)
        {
            if (_pendingText == null)
            {
                return;
            }

            if (AreEquivalent(_pendingText, text))
            {
                _pendingResult = result;
            }
            else if (IsStrictPrefix(_pendingText, text))
            {
                _pendingText = text;
                _pendingResult = result;
                _pendingWasExtended = true;
            }
            else if (AreSimilar(_pendingText, text))
            {
                _pendingText = text;
                _pendingResult = result;
                _pendingWasExtended = true;
            }
        }

        public void Reset()
        {
            _pendingText = null;
            _confirmedText = null;
            _pendingResult = default;
            _pendingWasExtended = false;
        }

        public void DiscardPending()
        {
            _pendingText = null;
            _pendingResult = default;
            _pendingWasExtended = false;
        }

        private ProgressiveTextCandidateUpdate<T> ConfirmPending()
        {
            _confirmedText = _pendingText;
            _pendingText = null;
            var result = _pendingResult;
            _pendingResult = default;
            _pendingWasExtended = false;
            return new(ProgressiveTextCandidateStatus.Confirmed, _confirmedText, result);
        }

        private static bool AreEquivalent(string? first, string? second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            var firstLength = GetComparisonLength(first);
            var secondLength = GetComparisonLength(second);
            return firstLength > 0
                && firstLength == secondLength
                && string.Compare(first, 0, second, 0, firstLength, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static bool IsStrictPrefix(string prefix, string text)
        {
            var prefixLength = GetComparisonLength(prefix);
            var textLength = GetComparisonLength(text);
            return prefixLength > 0
                && prefixLength < textLength
                && string.Compare(prefix, 0, text, 0, prefixLength, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static int GetComparisonLength(string text)
        {
            var end = text.Length;
            while (end > 0 && (char.IsWhiteSpace(text[end - 1]) || char.IsPunctuation(text[end - 1])))
            {
                end--;
            }

            return end;
        }

        private static bool AreSimilar(string first, string second)
        {
            var firstLength = GetComparisonLength(first);
            var secondLength = GetComparisonLength(second);
            if (Math.Min(firstLength, secondLength) < 30)
            {
                return false;
            }

            var shorter = firstLength <= secondLength ? first.AsSpan(0, firstLength) : second.AsSpan(0, secondLength);
            var longer = firstLength <= secondLength ? second.AsSpan(0, secondLength) : first.AsSpan(0, firstLength);
            if ((longer.Length - shorter.Length) / (double)longer.Length > 0.12)
            {
                return false;
            }

            var distances = ArrayPool<int>.Shared.Rent(shorter.Length + 1);
            try
            {
                for (var column = 0; column <= shorter.Length; column++)
                {
                    distances[column] = column;
                }

                for (var row = 1; row <= longer.Length; row++)
                {
                    var diagonal = distances[0];
                    distances[0] = row;
                    for (var column = 1; column <= shorter.Length; column++)
                    {
                        var previous = distances[column];
                        var substitutionCost = char.ToUpperInvariant(longer[row - 1]) == char.ToUpperInvariant(shorter[column - 1])
                            ? 0
                            : 1;
                        distances[column] = Math.Min(
                            Math.Min(distances[column] + 1, distances[column - 1] + 1),
                            diagonal + substitutionCost);
                        diagonal = previous;
                    }
                }

                var similarity = 1.0 - distances[shorter.Length] / (double)longer.Length;
                return similarity >= 0.91;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(distances);
            }
        }
    }
}
