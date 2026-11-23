using TraduSaga.Processing.TextProcessing;

namespace TraduSaga.Security.Tests;

public class ProgressiveTextCandidateTests
{
    [Fact]
    public void Observe_HoldsGrowingTextAndConfirmsTheLongestRepeatedValue()
    {
        var candidate = new ProgressiveTextCandidate();

        Assert.Equal(ProgressiveTextCandidateStatus.Pending, candidate.Observe("Years before", TimeSpan.Zero).Status);
        Assert.Equal(ProgressiveTextCandidateStatus.Pending, candidate.Observe("Years before the Grand Company", TimeSpan.FromMilliseconds(100)).Status);
        var confirmed = candidate.Observe("Years before the Grand Company", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("Years before the Grand Company", confirmed.Text);
    }

    [Fact]
    public void Observe_IgnoresFragmentsAfterTheFullTextWasConfirmed()
    {
        var candidate = new ProgressiveTextCandidate();
        candidate.Observe("Years before the Grand Company", TimeSpan.Zero);
        candidate.Observe("Years before the Grand Company", TimeSpan.FromMilliseconds(100));

        var fragment = candidate.Observe("Years before the Grand", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Ignored, fragment.Status);
        Assert.True(candidate.IsPrefixOfConfirmed("Years before the Grand"));
    }

    [Fact]
    public void Observe_KeepsDistinctTextThatSharesOnlyAnInitialPhrase()
    {
        var candidate = new ProgressiveTextCandidate();
        candidate.Observe("Years before the Grand Company", TimeSpan.Zero);
        var transition = candidate.Observe("Years later the Grand Company", TimeSpan.FromMilliseconds(100));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, transition.Status);
        Assert.Equal("Years before the Grand Company", transition.Text);
        Assert.Equal("Years later the Grand Company", candidate.PendingText);
    }

    [Fact]
    public void Observe_ConfirmsLatestTextAtTenSecondLimit()
    {
        var candidate = new ProgressiveTextCandidate();
        candidate.Observe("Years", TimeSpan.Zero);
        candidate.Observe("Years before", TimeSpan.FromSeconds(9));

        var confirmed = candidate.Observe("Years before the Grand Company", TimeSpan.FromSeconds(10));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("Years before the Grand Company", confirmed.Text);
    }

    [Fact]
    public void Observe_ConfirmsPendingTextAtTenSecondLimitWhenOcrHasNoValidText()
    {
        var candidate = new ProgressiveTextCandidate();
        candidate.Observe("Years before the Grand Company", TimeSpan.Zero);

        var confirmed = candidate.Observe(string.Empty, TimeSpan.FromSeconds(10));

        Assert.Equal(ProgressiveTextCandidateStatus.Confirmed, confirmed.Status);
        Assert.Equal("Years before the Grand Company", confirmed.Text);
    }

    [Fact]
    public void Observe_SuppressesRepeatedReadsOfTheConfirmedText()
    {
        var candidate = new ProgressiveTextCandidate();
        candidate.Observe("A complete sentence.", TimeSpan.Zero);
        candidate.Observe("A complete sentence.", TimeSpan.FromMilliseconds(100));

        var repeated = candidate.Observe("A complete sentence.", TimeSpan.FromMilliseconds(200));

        Assert.Equal(ProgressiveTextCandidateStatus.Ignored, repeated.Status);
    }
}
