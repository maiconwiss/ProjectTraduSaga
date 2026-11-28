using System.Collections.Generic;
using System.Linq;

namespace TraduSaga.Processing.TextProcessing
{
    internal static class SpeakerAreaDetectionResultSelector
    {
        public static TextDetectionResult PreferSpeakerAwareResult(
            TextDetectionResult preferredResult,
            IEnumerable<TextDetectionResult> candidates)
        {
            if (!string.IsNullOrWhiteSpace(preferredResult.SpeakerText))
                return preferredResult;

            return candidates
                .Where(result => !string.IsNullOrWhiteSpace(result.SpeakerText))
                .OrderByDescending(result => result.ValidityScore)
                .FirstOrDefault()
                ?? preferredResult;
        }
    }
}
