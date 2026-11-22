using System;
using System.IO;
using TraduSaga;

namespace TraduSaga.Security.Tests;

public class ApplicationLogPathTests
{
    [Fact]
    public void GetLogDirectoryPath_UsesTheCurrentUsersLocalApplicationData()
    {
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TraduSaga",
            "Logs");

        Assert.Equal(expectedPath, App.GetLogDirectoryPath());
    }
}
