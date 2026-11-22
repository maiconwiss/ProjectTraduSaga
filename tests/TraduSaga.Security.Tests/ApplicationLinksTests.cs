using TraduSaga.MVVM.Common;

namespace TraduSaga.Security.Tests;

public class ApplicationLinksTests
{
    [Fact]
    public void AboutLinksPointToTheSelectedRepository()
    {
        Assert.Equal("https://github.com/maiconwiss/ProjectTraduSaga", ApplicationLinks.ProjectRepository);
        Assert.Equal("https://github.com/maiconwiss/ProjectTraduSaga/issues/new", ApplicationLinks.ReportIssue);
        Assert.True(SafeExternalUrlPolicy.IsAllowed(ApplicationLinks.ProjectRepository));
        Assert.True(SafeExternalUrlPolicy.IsAllowed(ApplicationLinks.ReportIssue));
    }
}
