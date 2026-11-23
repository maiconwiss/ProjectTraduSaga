using TraduSaga.MVVM.Common;

namespace TraduSaga.Security.Tests;

public class SafeExternalUrlPolicyTests
{
    [Theory]
    [InlineData("https://github.com/maiconwiss/TraduSaga")]
    [InlineData("https://github.com/maiconwiss/TraduSaga/issues/new")]
    [InlineData("https://policies.google.com/privacy")]
    [InlineData("https://www.deepl.com/privacy")]
    [InlineData("https://yandex.com/legal/confidential/")]
    public void IsAllowed_AcceptsKnownHttpsHosts(string url)
    {
        Assert.True(SafeExternalUrlPolicy.IsAllowed(url));
    }

    [Theory]
    [InlineData("http://github.com/maiconwiss/TraduSaga")]
    [InlineData("https://github.com.attacker.invalid/")]
    [InlineData("https://user@github.com/")]
    [InlineData("https://github.com:444/")]
    [InlineData("https://example.invalid/")]
    public void IsAllowed_RejectsUntrustedUrls(string url)
    {
        Assert.False(SafeExternalUrlPolicy.IsAllowed(url));
    }
}
