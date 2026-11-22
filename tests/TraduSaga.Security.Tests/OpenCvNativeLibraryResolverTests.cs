using System;
using System.IO;
using System.Security.Cryptography;
using TraduSaga.OCR;

namespace TraduSaga.Security.Tests;

public class OpenCvNativeLibraryResolverTests
{
    [Fact]
    public void Register_InstallsResolversForThePinnedNativePackages()
    {
        OpenCvNativeLibraryResolver.Register();
    }

    [Fact]
    public void VerifyInstalledLibraries_AcceptsThePackagedNativeLibraries()
    {
        OpenCvNativeLibraryResolver.VerifyInstalledLibraries();
    }

    [Fact]
    public void GetNativeLibraryDirectory_UsesExecutableDirectoryForSingleFileApps()
    {
        var executableDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var processPath = Path.Combine(executableDirectory, "TraduSaga.exe");
        var extractionDirectory = Path.Combine(Path.GetTempPath(), "TraduSaga", "extracted");

        var directory = OpenCvNativeLibraryResolver.GetNativeLibraryDirectory(
            entryAssemblyName: "TraduSaga",
            processPath,
            extractionDirectory);

        Assert.Equal(executableDirectory, directory);
    }

    [Fact]
    public void GetNativeLibraryDirectory_UsesApplicationBaseForRegularAssemblies()
    {
        var entryAssemblyName = "TraduSaga";
        var processPath = Path.Combine(Path.GetTempPath(), "dotnet.exe");
        var applicationBaseDirectory = Path.Combine(Path.GetTempPath(), "TraduSaga");

        var directory = OpenCvNativeLibraryResolver.GetNativeLibraryDirectory(
            entryAssemblyName,
            processPath,
            applicationBaseDirectory);

        Assert.Equal(applicationBaseDirectory, directory);
    }

    [Fact]
    public void VerifyFile_AcceptsAFileWithTheExpectedHash()
    {
        var path = Path.Combine(Path.GetTempPath(), $"OpenCvSharpExtern-{Guid.NewGuid():N}.dll");
        var content = new byte[] { 1, 2, 3 };
        File.WriteAllBytes(path, content);

        try
        {
            OpenCvNativeLibraryResolver.VerifyFile(path, Convert.ToHexString(SHA256.HashData(content)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void VerifyFile_RejectsAChangedNativeLibrary()
    {
        var path = Path.Combine(Path.GetTempPath(), $"OpenCvSharpExtern-{Guid.NewGuid():N}.dll");
        File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

        try
        {
            Assert.Throws<FileLoadException>(() =>
                OpenCvNativeLibraryResolver.VerifyFile(path, Convert.ToHexString(SHA256.HashData(new byte[] { 3, 2, 1 }))));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
