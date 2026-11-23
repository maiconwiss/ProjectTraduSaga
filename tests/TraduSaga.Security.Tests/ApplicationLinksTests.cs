using TraduSaga.MVVM.Common;

namespace TraduSaga.Security.Tests;

public class ApplicationLinksTests
{
    [Fact]
    public void AboutLinksPointToTheSelectedRepository()
    {
        Assert.Equal("https://github.com/maiconwiss/TraduSaga", ApplicationLinks.ProjectRepository);
        Assert.Equal("https://github.com/maiconwiss/TraduSaga/issues/new", ApplicationLinks.ReportIssue);
        Assert.True(SafeExternalUrlPolicy.IsAllowed(ApplicationLinks.ProjectRepository));
        Assert.True(SafeExternalUrlPolicy.IsAllowed(ApplicationLinks.ReportIssue));
    }
}
