using System.Text.RegularExpressions;

namespace TraduSaga.Processing.TextProcessing
{
    internal static class ShortPunctuatedUtterancePolicy
    {
        public const float MinimumValidityScore = 2.2f;

        private static readonly Regex ShortPunctuatedUtterance = new Regex(
            @"^\s*\p{L}{1,3}\s*(?:\.{1,}|…+|[!?！？]+)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool TryGetValidityScore(string text, out float validityScore)
        {
            if (ShortPunctuatedUtterance.IsMatch(text))
            {
                validityScore = MinimumValidityScore;
                return true;
            }

            validityScore = 0;
            return false;
        }
    }
}
