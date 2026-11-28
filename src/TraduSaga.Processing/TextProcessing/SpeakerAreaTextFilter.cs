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
                    var centerX = captureArea.X + word.Bounds.X + word.Bounds.Width / 2;
                    var centerY = captureArea.Y + word.Bounds.Y + word.Bounds.Height / 2;
                    (speakerArea.Contains(centerX, centerY) ? speakerWords : dialogueWords).Add(word.Text);
                }

                if (speakerWords.Count > 0)
                    speaker.Add(string.Join(removeSpaces ? string.Empty : " ", speakerWords));
                if (dialogueWords.Count > 0)
                    dialogue.Add(dialogueWords.Count == line.Words.Count ? line.Text : JoinWords(dialogueWords, removeSpaces));
            }

            return (dialogue.ToArray(), string.Join(" ", speaker));
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
