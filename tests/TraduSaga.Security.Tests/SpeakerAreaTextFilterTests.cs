using System.Drawing;
using System.Threading.Tasks;
using TraduSaga.OCR;
using TraduSaga.Processing.TextProcessing;

namespace TraduSaga.Security.Tests;

public class SpeakerAreaTextFilterTests
{
    [Fact]
    public void Split_SeparatesWordsByTheirScreenCoordinatesAndRebuildsDialogue()
    {
        var lines = new[]
        {
            new OcrTextLine("Flame Mage", new[]
            {
                new OcrTextWord("Flame", new RectangleF(0, 0, 40, 12)),
                new OcrTextWord("Mage", new RectangleF(43, 0, 40, 12))
            }),
            new OcrTextLine("Hello, world!", new[]
            {
                new OcrTextWord("Hello,", new RectangleF(0, 50, 40, 12)),
                new OcrTextWord("world!", new RectangleF(43, 50, 40, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), new RectangleF(95, 195, 100, 30), false);

        Assert.Equal("Flame Mage", speaker);
        Assert.Equal(new[] { "Hello, world!" }, dialogue);
    }

    [Fact]
    public void Split_WhenNoWordFallsInSpeakerArea_KeepsDialogueAndReturnsNoLabel()
    {
        var lines = new[]
        {
            new OcrTextLine("Hello world", new[]
            {
                new OcrTextWord("Hello", new RectangleF(0, 0, 40, 12)),
                new OcrTextWord("world", new RectangleF(43, 0, 40, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), new RectangleF(70, 170, 20, 20), false);

        Assert.Equal("", speaker);
        Assert.Equal(new[] { "Hello world" }, dialogue);
    }

    [Fact]
    public void Split_WhenWordCenterIsOutsideButItsBoundsOverlapSpeakerArea_KeepsWholeNameAsLabel()
    {
        var lines = new[]
        {
            new OcrTextLine("Flame Mage", new[]
            {
                new OcrTextWord("Flame", new RectangleF(0, 0, 30, 12)),
                new OcrTextWord("Mage", new RectangleF(35, 0, 30, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), new RectangleF(96, 198, 14, 20), false);

        Assert.Equal("Flame", speaker);
        Assert.Equal(new[] { "Mage" }, dialogue);
    }

    [Fact]
    public void Split_RecognizesShortNameWhenItsBoundsOverlapSpeakerArea()
    {
        var lines = new[]
        {
            new OcrTextLine("Cid", new[]
            {
                new OcrTextWord("Cid", new RectangleF(0, 0, 9, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), new RectangleF(99, 198, 3, 20), false);

        Assert.Equal("Cid", speaker);
        Assert.Empty(dialogue);
    }

    [Fact]
    public void Split_WhenOnlyATinyEdgeOverlapsSpeakerArea_KeepsWordAsDialogue()
    {
        var lines = new[]
        {
            new OcrTextLine("Dialogue", new[]
            {
                new OcrTextWord("Dialogue", new RectangleF(0, 0, 30, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), new RectangleF(98, 198, 3, 20), false);

        Assert.Empty(speaker);
        Assert.Equal(new[] { "Dialogue" }, dialogue);
    }

    [Fact]
    public void Split_WhenAllWordsBelongToSpeakerArea_OmitsDialogueLine()
    {
        var lines = new[]
        {
            new OcrTextLine("Name", new[]
            {
                new OcrTextWord("Name", new RectangleF(0, 0, 40, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), new RectangleF(100, 200, 50, 20), false);

        Assert.Equal("Name", speaker);
        Assert.Empty(dialogue);
    }

    [Fact]
    public void Split_WhenLanguageRemovesSpaces_PreservesChineseAndJapaneseText()
    {
        var lines = new[]
        {
            new OcrTextLine("名前話し", new[]
            {
                new OcrTextWord("名", new RectangleF(0, 0, 10, 12)),
                new OcrTextWord("前", new RectangleF(11, 0, 10, 12)),
                new OcrTextWord("話", new RectangleF(22, 0, 10, 12)),
                new OcrTextWord("し", new RectangleF(33, 0, 10, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), new RectangleF(100, 200, 22, 20), true);

        Assert.Equal("名前", speaker);
        Assert.Equal(new[] { "話し" }, dialogue);
    }

    [Fact]
    public void Split_WhenSpeakerAreaIsEmpty_PreservesOriginalOcrLines()
    {
        var lines = new[]
        {
            new OcrTextLine("Flame Mage", new[]
            {
                new OcrTextWord("Flame", new RectangleF(0, 0, 40, 12)),
                new OcrTextWord("Mage", new RectangleF(43, 0, 40, 12))
            })
        };

        var (dialogue, speaker) = SpeakerAreaTextFilter.Split(
            lines, new RectangleF(100, 200, 400, 200), RectangleF.Empty, false);

        Assert.Equal(new[] { "Flame Mage" }, dialogue);
        Assert.Equal(string.Empty, speaker);
    }

    [Fact]
    public async Task TranslationOutputPipeline_TranslatesDialogueAndDisplaysOriginalLabelAboveSpeech()
    {
        var receivedByTranslator = string.Empty;
        var (translation, displayText) = await TranslationOutputPipeline.TranslateAsync(
            "Hello there", "Flame Mage", text =>
            {
                receivedByTranslator = text;
                return Task.FromResult("Olá");
            });
        var displayedText = string.Empty;
        var spokenText = string.Empty;

        TranslationOutputPipeline.Publish(translation, displayText,
            text => displayedText = text,
            text => spokenText = text);

        Assert.Equal("Hello there", receivedByTranslator);
        Assert.Equal($"Flame Mage{Environment.NewLine}Olá", displayedText);
        Assert.Equal("Olá", spokenText);
    }

    [Fact]
    public async Task TranslationOutputPipeline_WithoutSpeakerLabelDoesNotAddBlankLine()
    {
        var (_, displayText) = await TranslationOutputPipeline.TranslateAsync(
            "Hello there", string.Empty, text => Task.FromResult("Olá"));

        Assert.Equal("Olá", displayText);
    }
}
