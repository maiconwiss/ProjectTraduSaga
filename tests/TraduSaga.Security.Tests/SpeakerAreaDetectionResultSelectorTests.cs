using TraduSaga.Infrastructure.Language;
using TraduSaga.OCR;
using TraduSaga.Processing.TextProcessing;

namespace TraduSaga.Security.Tests;

public class SpeakerAreaDetectionResultSelectorTests
{
    [Fact]
    public void PreferSpeakerAwareResult_WhenBestResultContainsLabelInDialogue_UsesSeparatedDetection()
    {
        var preferred = CreateResult("Acracked Fist And so I ask", string.Empty, 4.8f);
        var separated = CreateResult("And so I ask", "Cracked Fist", 3.2f);

        var result = SpeakerAreaDetectionResultSelector.PreferSpeakerAwareResult(
            preferred, new[] { preferred, separated });

        Assert.Same(separated, result);
    }

    [Fact]
    public void PreferSpeakerAwareResult_WhenPreferredResultHasLabel_KeepsIt()
    {
        var preferred = CreateResult("And so I ask", "Cracked Fist", 3.2f);
        var other = CreateResult("And so I ask", "Flame Sergeant", 4.8f);

        var result = SpeakerAreaDetectionResultSelector.PreferSpeakerAwareResult(
            preferred, new[] { preferred, other });

        Assert.Same(preferred, result);
    }

    private static TextDetectionResult CreateResult(string text, string speaker, float score)
    {
        return new TextDetectionResult(new FakeOcrEngine(), new LanguageDescriptor())
        {
            Text = text,
            ValidatedText = text,
            SpeakerText = speaker,
            ValidityScore = score
        };
    }

    private sealed class FakeOcrEngine : IOCREngine
    {
        public byte PrimaryPriority => 0;
        public bool SecondaryPrimaryCheck => false;
        public int Confidence => 0;
        public Languages DetectionLanguage => Languages.English;
        public OcrTextLine[] GetTextLines(byte[] image) => [];
    }
}
