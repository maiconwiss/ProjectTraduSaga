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
    public void Observe_IgnoresFragmentsAfterTheFullTextWasConfirmed()
    {
        var candidate = new ProgressiveTextCandidate<string>();
        candidate.Observe("Years before the Grand Company", "complete", TimeSpan.Zero);
        candidate.Observe("Years before the Grand Company", "repeat", TimeSpan.FromMilliseconds(100));

        var fragment = candidate.Observe("Years before the Grand", "fragment", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Ignored, fragment.Status);
        Assert.True(candidate.IsPrefixOfConfirmed("Years before the Grand"));
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
