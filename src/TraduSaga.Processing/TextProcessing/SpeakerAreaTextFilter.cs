using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TraduSaga.OCR;

namespace TraduSaga.Processing.TextProcessing
{
    internal static class SpeakerAreaTextFilter
    {
        private static readonly Regex PunctuationAtStart = new Regex(@"^[,.;:!?%\)\]\}]", RegexOptions.Compiled);
        private const float MINIMUM_WORD_AREA_OVERLAP = 0.15f;

        public static (string[] DialogueLines, string SpeakerText) Split(
            OcrTextLine[] lines, RectangleF captureArea, RectangleF speakerArea, bool removeSpaces)
        {
            if (speakerArea.IsEmpty)
                return (lines.Select(line => line.Text).ToArray(), string.Empty);

            var dialogue = new List<string>();
            var speaker = new List<string>();
            foreach (var line in lines)
            {
                var dialogueWords = new List<string>();
                var speakerWords = new List<string>();
                foreach (var word in line.Words)
                {
                    (BelongsToSpeakerArea(word.Bounds, captureArea, speakerArea) ? speakerWords : dialogueWords).Add(word.Text);
                }

                if (speakerWords.Count > 0)
                    speaker.Add(string.Join(removeSpaces ? string.Empty : " ", speakerWords));
                if (dialogueWords.Count > 0)
                    dialogue.Add(dialogueWords.Count == line.Words.Count ? line.Text : JoinWords(dialogueWords, removeSpaces));
            }

            return (dialogue.ToArray(), string.Join(" ", speaker));
        }

        private static bool BelongsToSpeakerArea(RectangleF wordBounds, RectangleF captureArea, RectangleF speakerArea)
        {
            var screenBounds = new RectangleF(
                captureArea.X + wordBounds.X,
                captureArea.Y + wordBounds.Y,
                wordBounds.Width,
                wordBounds.Height);
            if (speakerArea.Contains(screenBounds.Left + screenBounds.Width / 2, screenBounds.Top + screenBounds.Height / 2))
                return true;

            var wordArea = screenBounds.Width * screenBounds.Height;
            if (wordArea <= 0)
                return false;

            var overlap = RectangleF.Intersect(screenBounds, speakerArea);
            return !overlap.IsEmpty && overlap.Width * overlap.Height / wordArea >= MINIMUM_WORD_AREA_OVERLAP;
        }

        private static string JoinWords(IReadOnlyList<string> words, bool removeSpaces)
        {
            var result = new StringBuilder();
            foreach (var word in words)
            {
                if (!removeSpaces && result.Length > 0 && !PunctuationAtStart.IsMatch(word))
                    result.Append(' ');
                result.Append(word);
            }
            return result.ToString();
        }
    }
}
