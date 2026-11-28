using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using TraduSaga.Configuration;
using TraduSaga.Infrastructure.Encryption;

namespace TraduSaga.Security.Tests;

public class WindowSettingsPersistenceTests
{
    [Fact]
    public void ConfigurationStorage_RestoresCaptureAreaAndWindowGeometry()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TraduSaga.Security.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "settings");
            var protection = new DpapiDataProtectionService();
            var secretStore = new DpapiSecretStore(protection, Path.Combine(directory, "secrets"));
            var capture = new ScreenCaptureConfiguration
            {
                CaptureArea = new RectangleF(12, 34, 500, 300),
                SpeakerArea = new RectangleF(30, 45, 80, 24)
            };
            var geometry = new TranslationWindowGeometry { Left = 123, Top = 234, Width = 600, Height = 320 };
            var storage = CreateStorage(capture, geometry, path, protection, secretStore);
            Assert.True(storage.LoadConfiguration());
            storage.SaveConfiguration();

            var restoredCapture = new ScreenCaptureConfiguration();
            var restoredGeometry = new TranslationWindowGeometry();
            var restoredStorage = CreateStorage(restoredCapture, restoredGeometry, path, protection, secretStore);

            Assert.True(restoredStorage.LoadConfiguration());
            Assert.Equal(new RectangleF(12, 34, 500, 300), restoredCapture.CaptureArea);
            Assert.Equal(new RectangleF(30, 45, 80, 24), restoredCapture.SpeakerArea);
            Assert.Equal(123, restoredGeometry.Left);
            Assert.Equal(234, restoredGeometry.Top);
            Assert.Equal(600, restoredGeometry.Width);
            Assert.Equal(320, restoredGeometry.Height);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ConfigurationStorage_AutomaticallyPersistsCaptureAreaChanges()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TraduSaga.Security.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "settings");
            var protection = new DpapiDataProtectionService();
            var secretStore = new DpapiSecretStore(protection, Path.Combine(directory, "secrets"));
            var capture = new ScreenCaptureConfiguration();
            var geometry = new TranslationWindowGeometry();
            var storage = CreateStorage(capture, geometry, path, protection, secretStore);
            Assert.True(storage.LoadConfiguration());

            capture.CaptureArea = new RectangleF(15, 25, 350, 240);
            capture.SpeakerArea = new RectangleF(20, 32, 80, 24);
            await WaitForSettingsFile(path);

            var restoredCapture = new ScreenCaptureConfiguration();
            var restoredGeometry = new TranslationWindowGeometry();
            var restoredStorage = CreateStorage(restoredCapture, restoredGeometry, path, protection, secretStore);
            Assert.True(restoredStorage.LoadConfiguration());
            Assert.Equal(new RectangleF(15, 25, 350, 240), restoredCapture.CaptureArea);
            Assert.Equal(new RectangleF(20, 32, 80, 24), restoredCapture.SpeakerArea);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ConfigurationStorage_AutomaticallyPersistsWindowGeometryChanges()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TraduSaga.Security.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "settings");
            var protection = new DpapiDataProtectionService();
            var secretStore = new DpapiSecretStore(protection, Path.Combine(directory, "secrets"));
            var capture = new ScreenCaptureConfiguration();
            var geometry = new TranslationWindowGeometry();
            var storage = CreateStorage(capture, geometry, path, protection, secretStore);
            Assert.True(storage.LoadConfiguration());

            geometry.Left = 140;
            geometry.Top = 240;
            geometry.Width = 620;
            geometry.Height = 360;
            await WaitForSettingsFile(path);

            var restoredCapture = new ScreenCaptureConfiguration();
            var restoredGeometry = new TranslationWindowGeometry();
            var restoredStorage = CreateStorage(restoredCapture, restoredGeometry, path, protection, secretStore);
            Assert.True(restoredStorage.LoadConfiguration());
            Assert.Equal(140, restoredGeometry.Left);
            Assert.Equal(240, restoredGeometry.Top);
            Assert.Equal(620, restoredGeometry.Width);
            Assert.Equal(360, restoredGeometry.Height);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task WaitForSettingsFile(string path)
    {
        for (var attempt = 0; attempt < 60 && !File.Exists(path); attempt++)
        {
            await Task.Delay(50);
        }

        Assert.True(File.Exists(path), "The configuration change was not automatically persisted.");
    }

    private static ConfigurationStorage CreateStorage(
        ScreenCaptureConfiguration capture,
        TranslationWindowGeometry geometry,
        string path,
        IDataProtectionService protection,
        ISecretStore secretStore)
    {
        var services = new TestServiceProvider(capture, geometry);
        var storage = new ConfigurationStorage(services, protection, secretStore,
            NullLogger<ConfigurationStorage>.Instance, path);
        storage.RegisterConfiguration<ScreenCaptureConfiguration>();
        storage.RegisterConfiguration<TranslationWindowGeometry>();
        return storage;
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly ScreenCaptureConfiguration _capture;
        private readonly TranslationWindowGeometry _geometry;

        public TestServiceProvider(ScreenCaptureConfiguration capture, TranslationWindowGeometry geometry)
        {
            _capture = capture;
            _geometry = geometry;
        }

        public object? GetService(Type serviceType) => serviceType == typeof(ScreenCaptureConfiguration)
            ? _capture
            : serviceType == typeof(TranslationWindowGeometry) ? _geometry : null;
    }
}
