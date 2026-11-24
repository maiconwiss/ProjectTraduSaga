using TraduSaga.Processing.TextProcessing;

namespace TraduSaga.Security.Tests;

public class ProgressiveTextCandidateTests
{
    [Fact]
    public void Observe_HoldsGrowingTextAndConfirmsTheLongestRepeatedValue()
    {
        var candidate = new ProgressiveTextCandidate<string>();

        Assert.Equal(ProgressiveTextCandidateStatus.Pending, candidate.Observe("Years before", "partial", TimeSpan.Zero).Status);
        Assert.Equal(ProgressiveTextCandidateStatus.Pending, candidate.Observe("Years before the Grand Company", "complete", TimeSpan.FromMilliseconds(100)).Status);
        var confirmed = candidate.Observe("Years before the Grand Company", "repeat", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("Years before the Grand Company", confirmed.Text);
        Assert.Equal("complete", confirmed.Result);
    }

    [Fact]
    public void Observe_TreatsAutoAddedEndPunctuationAsAProgressivePrefix()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Before one might understand.", "partial", TimeSpan.Zero);

        var extension = candidate.Observe(
            "Before one might understand aetherial travel, one must first understand the aether.",
            "complete",
            TimeSpan.FromMilliseconds(100));

        Assert.Equal(ProgressiveTextCandidateStatus.Pending, extension.Status);
        Assert.Equal("Before one might understand aetherial travel, one must first understand the aether.", extension.Text);

        var confirmed = candidate.Observe(
            "Before one might understand aetherial travel, one must first understand the aether.",
            "repeat",
            TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("Before one might understand aetherial travel, one must first understand the aether.", confirmed.Text);
        Assert.Equal("complete", confirmed.Result);
    }

    [Fact]
    public void Observe_IgnoresFragmentsAfterTheFullTextWasConfirmed()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years before the Grand Company", "complete", TimeSpan.Zero);
        candidate.Observe("Years before the Grand Company", "repeat", TimeSpan.FromMilliseconds(100));

        var fragment = candidate.Observe("Years before the Grand", "fragment", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Ignored, fragment.Status);
        Assert.True(candidate.IsDuplicateOfConfirmed("Years before the Grand"));
    }

    [Fact]
    public void Observe_IgnoresAFragmentWithAutoAddedEndPunctuationAfterConfirmation()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Before one might understand aetherial travel.", "complete", TimeSpan.Zero);
        candidate.Observe("Before one might understand aetherial travel.", "repeat", TimeSpan.FromMilliseconds(100));

        var fragment = candidate.Observe("Before one might understand.", "partial", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Ignored, fragment.Status);
    }

    [Fact]
    public void Observe_TreatsMinorOcrCorrectionsInALongDialogueAsOneCandidate()
    {
        const string firstRead = "Greetings adventurer I was appointed to this post to answer questions about aetherial travel";
        const string correctedRead = "Greetings adventurer I was assigned to this post to answer questions about aetherial travel";
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe(firstRead, "first", TimeSpan.Zero);

        var correction = candidate.Observe(correctedRead, "corrected", TimeSpan.FromMilliseconds(100));

        Assert.Equal(ProgressiveTextCandidateStatus.Pending, correction.Status);
        Assert.True(candidate.PendingWasExtended);
        Assert.Equal(correctedRead, correction.Text);

        var confirmed = candidate.Observe(correctedRead, "repeat", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("corrected", confirmed.Result);
    }

    [Fact]
    public void Observe_PreservesDistinctDialogueWithSharedOpeningWords()
    {
        const string firstLine = "Before one might understand aetherial travel, one must study its origin.";
        const string secondLine = "Before one might understand our purpose, one must hear the captain's story.";
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe(firstLine, "first", TimeSpan.Zero);
        var transition = candidate.Observe(secondLine, "second", TimeSpan.FromMilliseconds(100));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, transition.Status);
        Assert.Equal(firstLine, transition.Text);
        Assert.Equal(secondLine, candidate.PendingText);
    }

    [Fact]
    public void Observe_KeepsDistinctTextThatSharesOnlyAnInitialPhrase()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years before the Grand Company", "first", TimeSpan.Zero);
        var transition = candidate.Observe("Years later the Grand Company", "second", TimeSpan.FromMilliseconds(100));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, transition.Status);
        Assert.Equal("Years before the Grand Company", transition.Text);
        Assert.Equal("first", transition.Result);
        Assert.Equal("Years later the Grand Company", candidate.PendingText);
    }

    [Fact]
    public void Observe_ConfirmsLatestTextAtTenSecondLimit()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years", "first", TimeSpan.Zero);
        candidate.Observe("Years before", "second", TimeSpan.FromSeconds(9));

        var confirmed = candidate.Observe("Years before the Grand Company", "latest", TimeSpan.FromSeconds(10));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("Years before the Grand Company", confirmed.Text);
        Assert.Equal("latest", confirmed.Result);
    }

    [Fact]
    public void Observe_ConfirmsPendingTextAtTenSecondLimitWhenOcrHasNoValidText()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years before the Grand Company", "complete", TimeSpan.Zero);

        var confirmed = candidate.Observe(string.Empty, null, TimeSpan.FromSeconds(10));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("Years before the Grand Company", confirmed.Text);
        Assert.Equal("complete", confirmed.Result);
    }

    [Fact]
    public void Observe_SuppressesRepeatedReadsOfTheConfirmedText()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("A complete sentence.", "first", TimeSpan.Zero);
        var confirmed = candidate.Observe("A complete sentence.", "repeat", TimeSpan.FromMilliseconds(100));

        var repeated = candidate.Observe("A complete sentence.", "repeat again", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal(ProgressiveTextCandidateStatus.Ignored, repeated.Status);
    }

    [Fact]
    public void UpdatePendingWithBestText_KeepsThePrimaryResultWhenBestTextIsShorter()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years before the Grand Company", "primary", TimeSpan.Zero);
        candidate.UpdatePendingWithBestText("Years before", "shorter-best");

        var confirmed = candidate.Observe("Years before the Grand Company", "repeat", TimeSpan.FromMilliseconds(100));

        Assert.Equal("Years before the Grand Company", confirmed.Text);
        Assert.Equal("primary", confirmed.Result);
    }

    [Fact]
    public void RemainingWait_IsBoundedByTenSecondsFromTheFirstObservation()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years", "primary", TimeSpan.Zero);
        candidate.Observe("Years before", "extended", TimeSpan.FromSeconds(9));

        Assert.Equal(TimeSpan.FromSeconds(1), candidate.RemainingWait(TimeSpan.FromSeconds(9)));
    }

    [Fact]
    public void DiscardPending_DoesNotLeaveAResultThatCanBeTranslated()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years before", "partial", TimeSpan.Zero);
        candidate.DiscardPending();

        var next = candidate.Observe("A different line", "new", TimeSpan.FromMilliseconds(100));

        Assert.Equal(ProgressiveTextCandidateStatus.Pending, next.Status);
        Assert.Equal("new", next.Result);
    }
}
