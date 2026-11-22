using System.Diagnostics;
using System.IO;
using TraduSaga.OCR.WindowsOCR;
using TraduSaga.TTS;

namespace TraduSaga.Security.Tests;

public class WindowsCapabilityInstallProcessTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    [InlineData("pt-BR")]
    public void OcrInstall_UsesFixedScriptAndSeparateSupportedTag(string languageTag)
    {
        var startInfo = WindowsOCRHelper.CreateOcrCapabilityInstallStartInfo(languageTag);

        AssertInstallStartInfo(startInfo, languageTag, "Language.OCR~~~");
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    [InlineData("pt-BR")]
    public void TtsInstall_UsesFixedScriptAndSeparateSupportedTag(string languageTag)
    {
        var startInfo = WindowsTTSHelper.CreateTTSCapabilityInstallStartInfo(languageTag);

        AssertInstallStartInfo(startInfo, languageTag, "Language.TextToSpeech~~~");
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US'")]
    [InlineData("en-US\"")]
    [InlineData("en-US; Write-Output rejected")]
    [InlineData("en-US`x")]
    [InlineData("en-US\n")]
    [InlineData("en-US&value")]
    [InlineData("en-US|value")]
    [InlineData("en-US$()")]
    [InlineData("en-US path")]
    [InlineData("../../outside")]
    [InlineData("\uFF45n-US")]
    [InlineData("zz-ZZ")]
    public async Task InvalidTags_AreRejectedWithoutCreatingAProcess(string languageTag)
    {
        Assert.Null(WindowsOCRHelper.CreateOcrCapabilityInstallStartInfo(languageTag));
        Assert.Null(WindowsTTSHelper.CreateTTSCapabilityInstallStartInfo(languageTag));
        await Assert.ThrowsAsync<ArgumentException>(() => WindowsOCRHelper.InstallOcrLanguageCapability(languageTag));
        await Assert.ThrowsAsync<ArgumentException>(() => WindowsTTSHelper.InstallTTSLanguageCapability(languageTag));
    }

    [Fact]
    public void OversizedTag_IsRejectedWithoutCreatingAProcess()
    {
        var languageTag = new string('a', 4096);

        Assert.Null(WindowsOCRHelper.CreateOcrCapabilityInstallStartInfo(languageTag));
        Assert.Null(WindowsTTSHelper.CreateTTSCapabilityInstallStartInfo(languageTag));
    }

    [Fact]
    public void ProcessArguments_PassTheTagSeparatelyToPowerShell()
    {
        var startInfo = WindowsOCRHelper.CreateOcrCapabilityInstallStartInfo("en-US");
        startInfo.UseShellExecute = false;
        startInfo.Verb = string.Empty;
        startInfo.RedirectStandardOutput = true;
        startInfo.ArgumentList[3] = "& { param([string]$tag) Write-Output $tag }";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("PowerShell did not start.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Assert.Equal(0, process.ExitCode);
        Assert.Equal("en-US", output.Trim());
    }

    [Fact]
    public void AvailabilityChecks_DoNotStartCapabilityProcesses()
    {
        var previousOcrStarter = WindowsOCRHelper.CapabilityProcessStarter;
        var previousTtsStarter = WindowsTTSHelper.CapabilityProcessStarter;
        var processStartCount = 0;
        Process RecordProcessStart(ProcessStartInfo _)
        {
            processStartCount++;
            throw new InvalidOperationException("An availability check attempted to start a capability process.");
        }

        try
        {
            WindowsOCRHelper.CapabilityProcessStarter = RecordProcessStart;
            WindowsTTSHelper.CapabilityProcessStarter = RecordProcessStart;

            WindowsOCRHelper.IsLanguageOcrCapabilityInstalled("en-US");
            WindowsTTSHelper.IsLanguageTTSCapabilityInstalled("en-US");

            Assert.Equal(0, processStartCount);
        }
        finally
        {
            WindowsOCRHelper.CapabilityProcessStarter = previousOcrStarter;
            WindowsTTSHelper.CapabilityProcessStarter = previousTtsStarter;
        }
    }

    private static void AssertInstallStartInfo(ProcessStartInfo startInfo, string languageTag, string capabilityPrefix)
    {
        Assert.True(Path.IsPathFullyQualified(startInfo.FileName));
        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
            startInfo.FileName);
        Assert.True(startInfo.UseShellExecute);
        Assert.Equal("runas", startInfo.Verb);
        Assert.Equal("-NoProfile", startInfo.ArgumentList[0]);
        Assert.Equal("-NonInteractive", startInfo.ArgumentList[1]);
        Assert.Equal("-Command", startInfo.ArgumentList[2]);
        Assert.Contains(capabilityPrefix, startInfo.ArgumentList[3], StringComparison.Ordinal);
        Assert.DoesNotContain(languageTag, startInfo.ArgumentList[3], StringComparison.Ordinal);
        Assert.Equal(languageTag, startInfo.ArgumentList[4]);
    }
}
