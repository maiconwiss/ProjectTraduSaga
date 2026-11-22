#nullable enable

using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using OpenCvSharp;

namespace TraduSaga.OCR;

public static class OpenCvNativeLibraryResolver
{
    private const string ManifestResourceName = "TraduSaga.OCR.native-library-manifest.json";
    private static readonly Lazy<Dictionary<string, NativeLibraryManifest>> Manifest = new(LoadManifest);
    private static int _registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(typeof(Cv2).Assembly, ResolveNativeLibrary);
        NativeLibrary.SetDllImportResolver(
            Assembly.Load(new AssemblyName("Microsoft.ML.Transforms")),
            ResolveNativeLibrary);
    }

    public static void VerifyInstalledLibraries()
    {
        var libraryDirectory = GetNativeLibraryDirectory();
        foreach (var library in Manifest.Value.Values)
        {
            VerifyFile(Path.Combine(libraryDirectory, library.FileName), library.Sha256);
        }
    }

    private static string GetNativeLibraryDirectory() =>
        GetNativeLibraryDirectory(
            Assembly.GetEntryAssembly()?.GetName().Name,
            Environment.ProcessPath,
            AppContext.BaseDirectory);

    internal static string GetNativeLibraryDirectory(
        string? entryAssemblyName,
        string? processPath,
        string applicationBaseDirectory)
    {
        if (string.IsNullOrWhiteSpace(entryAssemblyName)
            || string.IsNullOrWhiteSpace(processPath)
            || !Path.IsPathFullyQualified(processPath)
            || !string.Equals(
                Path.GetFileNameWithoutExtension(processPath),
                entryAssemblyName,
                StringComparison.OrdinalIgnoreCase))
        {
            return applicationBaseDirectory;
        }

        return Path.GetDirectoryName(processPath) ?? applicationBaseDirectory;
    }

    internal static void VerifyFile(string path, string expectedSha256)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            throw new InvalidDataException("The native library path is not absolute.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The required native library is missing.", path);
        }

        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("The native library cannot be a reparse point.");
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var actualSha256 = Convert.ToHexString(SHA256.HashData(stream));
        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileLoadException("The native library does not match the pinned package hash.", path);
        }
    }

    private static IntPtr ResolveNativeLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        var libraryFileName = Path.GetFileNameWithoutExtension(libraryName);
        if (!Manifest.Value.TryGetValue(libraryFileName, out var library))
        {
            return IntPtr.Zero;
        }

        var path = Path.Combine(GetNativeLibraryDirectory(), library.FileName);
        VerifyFile(path, library.Sha256);
        return NativeLibrary.Load(path);
    }

    private static Dictionary<string, NativeLibraryManifest> LoadManifest()
    {
        using var resource = typeof(OpenCvNativeLibraryResolver).Assembly
            .GetManifestResourceStream(ManifestResourceName)
            ?? throw new InvalidOperationException("The native library manifest is missing.");
        using var document = JsonDocument.Parse(resource);
        var root = document.RootElement;
        var libraries = new Dictionary<string, NativeLibraryManifest>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in root.GetProperty("libraries").EnumerateArray())
        {
            var libraryName = item.GetProperty("libraryName").GetString();
            var fileName = item.GetProperty("file").GetString();
            var sha256 = item.GetProperty("sha256").GetString();

            if (string.IsNullOrWhiteSpace(libraryName)
                || string.IsNullOrWhiteSpace(fileName)
                || Path.GetFileName(fileName) != fileName
                || string.IsNullOrWhiteSpace(sha256)
                || sha256.Length != 64
                || !sha256.All(Uri.IsHexDigit)
                || !libraries.TryAdd(libraryName, new NativeLibraryManifest(fileName, sha256)))
            {
                throw new InvalidDataException("The native library manifest is invalid.");
            }
        }

        if (libraries.Count == 0)
        {
            throw new InvalidDataException("The native library manifest contains no libraries.");
        }

        return libraries;
    }

    private sealed record NativeLibraryManifest(string FileName, string Sha256);
}
