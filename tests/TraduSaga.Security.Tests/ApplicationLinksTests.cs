using TraduSaga.MVVM.Common;

namespace TraduSaga.Security.Tests;

public class ApplicationLinksTests
{
    [Fact]
    public void AboutLinksPointToTheSelectedRepository()
    {
        Assert.Equal("https://github.com/maiconwiss/traducaoRPG", ApplicationLinks.ProjectRepository);
        Assert.Equal("https://github.com/maiconwiss/traducaoRPG/issues/new", ApplicationLinks.ReportIssue);
        Assert.True(SafeExternalUrlPolicy.IsAllowed(ApplicationLinks.ProjectRepository));
        Assert.True(SafeExternalUrlPolicy.IsAllowed(ApplicationLinks.ReportIssue));
    }
}
