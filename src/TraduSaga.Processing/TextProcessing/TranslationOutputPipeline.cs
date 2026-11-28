using System;
using System.Threading.Tasks;

namespace TraduSaga.Processing.TextProcessing
{
    internal static class TranslationOutputPipeline
    {
        public static async Task<(string Translation, string DisplayText)> TranslateAsync(
            string dialogueText, string speakerText, Func<string, Task<string>> translate)
        {
            var translation = await translate(dialogueText).ConfigureAwait(false);
            var displayText = string.IsNullOrWhiteSpace(speakerText)
                ? translation
                : speakerText + Environment.NewLine + translation;
            return (translation, displayText);
        }

        public static void Publish(string translation, string displayText, Action<string> display, Action<string> speak)
        {
            display(displayText);
            speak(translation);
        }
    }
}
