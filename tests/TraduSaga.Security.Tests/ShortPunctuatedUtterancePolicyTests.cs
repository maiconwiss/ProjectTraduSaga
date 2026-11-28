using TraduSaga.Processing.TextProcessing;

namespace TraduSaga.Security.Tests;

public class ShortPunctuatedUtterancePolicyTests
{
    [Theory]
    [InlineData("I...")]
    [InlineData("No!")]
    [InlineData("Ah?")]
    public void TryGetValidityScore_AcceptsShortPunctuatedDialogue(string text)
    {
        var accepted = ShortPunctuatedUtterancePolicy.TryGetValidityScore(text, out var score);

        Assert.True(accepted);
        Assert.Equal(2.2f, score);
    }

    [Theory]
    [InlineData("Cid")]
    [InlineData("...")]
    [InlineData("I am here.")]
    [InlineData("????")]
    public void TryGetValidityScore_LeavesOtherTextToTheModel(string text)
    {
        var accepted = ShortPunctuatedUtterancePolicy.TryGetValidityScore(text, out var score);

        Assert.False(accepted);
        Assert.Equal(0, score);
    }
}
