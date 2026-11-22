using TraduSaga.OCR.Configuration;
using TraduSaga.OCR.WindowsOCR;

namespace TraduSaga.Security.Tests;

public class OcrConfigurationTests
{
    [Fact]
    public void DefaultConfigurationContainsOnlyWindowsOcr()
    {
        var configurations = OcrGeneralConfiguration.Default.OcrConfigurations;

        Assert.Single(configurations);
        Assert.IsType<WindowsOCRConfiguration>(configurations[0]);
    }
}
