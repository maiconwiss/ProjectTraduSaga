using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TraduSaga.Configuration;
using TraduSaga.Infrastructure.Encryption;
using TraduSaga.Translation.Configuration;

namespace TraduSaga.Security.Tests;

public class ConfigurationProtectionTests
{
    [Fact]
    public void DefaultStoragePathsAndProtectionPurposesUseTraduSagaIdentity()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        Assert.Equal(Path.Combine(localAppData, "TraduSaga", "settings"),
            ConfigurationStorage.GetDefaultConfigurationPath(Environment.SpecialFolder.LocalApplicationData));
        Assert.Equal(Path.Combine(appData, "TraduSaga", "settings"),
            ConfigurationStorage.GetDefaultConfigurationPath(Environment.SpecialFolder.ApplicationData));
        Assert.Equal(Path.Combine(localAppData, "TraduSaga", "Secrets"), DpapiSecretStore.GetDefaultDirectoryPath());
        Assert.Equal("TraduSaga.Settings.v1", ConfigurationStorage.SETTINGS_PROTECTION_PURPOSE);
        Assert.Equal("TraduSaga.Secret.v1.deepl-api-key", DpapiSecretStore.GetPurpose(DpapiSecretStore.DeepLApiKeyName));
    }

    [Fact]
    public void DataProtection_RoundTripsForCurrentUser()
    {
        var protector = new DpapiDataProtectionService();
        var plaintext = Encoding.UTF8.GetBytes("private value");

        var protectedValue = protector.Protect(plaintext, "test-purpose");

        Assert.Equal(plaintext, protector.Unprotect(protectedValue, "test-purpose"));
    }

    [Fact]
    public void DataProtection_RejectsModifiedCiphertext()
    {
        var protector = new DpapiDataProtectionService();
        var protectedValue = protector.Protect(Encoding.UTF8.GetBytes("private value"), "test-purpose");
        protectedValue[^1] ^= 0x80;

        Assert.Throws<CryptographicException>(() => protector.Unprotect(protectedValue, "test-purpose"));
    }

    [Fact]
    public void SecretStore_ProtectsValuesAndRejectsUnknownNames()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var store = new DpapiSecretStore(new DpapiDataProtectionService(), directory);
            const string secret = "test-api-key-4e27";

            store.SetSecret(DpapiSecretStore.DeepLApiKeyName, secret);
            var protectedValue = File.ReadAllBytes(Path.Combine(directory, DpapiSecretStore.DeepLApiKeyName + ".bin"));

            Assert.Equal(secret, store.GetSecret(DpapiSecretStore.DeepLApiKeyName));
            Assert.True(protectedValue.AsSpan().IndexOf(Encoding.UTF8.GetBytes(secret)) < 0);
            Assert.Throws<ArgumentException>(() => store.SetSecret("..\\outside", secret));

            var security = new DirectoryInfo(directory).GetAccessControl();
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .ToArray();
            var currentUser = WindowsIdentity.GetCurrent().User;
            var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

            Assert.True(security.AreAccessRulesProtected);
            Assert.Contains(rules, rule => rule.IdentityReference == currentUser);
            Assert.DoesNotContain(rules, rule => rule.IdentityReference == world && rule.AccessControlType == AccessControlType.Allow);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ConfigurationStorage_ProtectsSettingsAndKeepsKeysOutOfSerializedData()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var configurationPath = Path.Combine(directory, "settings");
            var dataProtectionService = new DpapiDataProtectionService();
            var secretStore = new DpapiSecretStore(dataProtectionService, Path.Combine(directory, "secrets"));
            var configuration = new TranslationConfiguration();
            var storage = CreateStorage(configuration, configurationPath, dataProtectionService, secretStore);
            Assert.True(storage.LoadConfiguration());
            configuration.DeeplApiKey = "deepL-test-key-7d41";
            configuration.YandexApiKey = "yandex-test-key-2a98";

            storage.SaveConfiguration();
            var fileContents = File.ReadAllBytes(configurationPath);

            Assert.False(fileContents.AsSpan().IndexOf(Encoding.UTF8.GetBytes(configuration.DeeplApiKey)) >= 0);
            Assert.False(fileContents.AsSpan().IndexOf(Encoding.UTF8.GetBytes(configuration.YandexApiKey)) >= 0);

            var loadedConfiguration = new TranslationConfiguration();
            var loadedStorage = CreateStorage(loadedConfiguration, configurationPath, dataProtectionService, secretStore);
            Assert.True(loadedStorage.LoadConfiguration());
            Assert.Equal(configuration.DeeplApiKey, loadedConfiguration.DeeplApiKey);
            Assert.Equal(configuration.YandexApiKey, loadedConfiguration.YandexApiKey);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ConfigurationStorage_PreservesUnsupportedLegacyFileAndBlocksWrites()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var configurationPath = Path.Combine(directory, "settings");
            var legacyContents = new byte[] { 1, 2, 3, 4, 5 };
            File.WriteAllBytes(configurationPath, legacyContents);

            var storage = CreateStorage(
                new TranslationConfiguration(),
                configurationPath,
                new DpapiDataProtectionService(),
                new DpapiSecretStore(new DpapiDataProtectionService(), Path.Combine(directory, "secrets")));

            Assert.False(storage.LoadConfiguration());
            Assert.False(storage.CanSaveConfiguration);
            storage.SaveConfiguration();

            Assert.Equal(legacyContents, File.ReadAllBytes(configurationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ConfigurationStorage_RejectsTamperingWithoutOverwritingSource()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var configurationPath = Path.Combine(directory, "settings");
            var dataProtectionService = new DpapiDataProtectionService();
            var secretStore = new DpapiSecretStore(dataProtectionService, Path.Combine(directory, "secrets"));
            var storage = CreateStorage(new TranslationConfiguration(), configurationPath, dataProtectionService, secretStore);
            Assert.True(storage.LoadConfiguration());
            storage.SaveConfiguration();

            var original = File.ReadAllBytes(configurationPath);
            original[^1] ^= 0x40;
            File.WriteAllBytes(configurationPath, original);

            var corrupted = File.ReadAllBytes(configurationPath);
            var reload = CreateStorage(new TranslationConfiguration(), configurationPath, dataProtectionService, secretStore);

            Assert.False(reload.LoadConfiguration());
            reload.SaveConfiguration();
            Assert.Equal(corrupted, File.ReadAllBytes(configurationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><anyType xsi:type=\"UnregisteredConfiguration\" /></ArrayOfAnyType>")]
    [InlineData("<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:evil=\"urn:evil\"><anyType xsi:type=\"evil:TranslationConfiguration\" /></ArrayOfAnyType>")]
    public void ConfigurationStorage_RejectsUnregisteredXmlTypesWithoutOverwritingSource(string xmlDocument)
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var configurationPath = Path.Combine(directory, "settings");
            var protector = new DpapiDataProtectionService();
            var settingsBytes = CreateProtectedSettings(xmlDocument, protector);
            File.WriteAllBytes(configurationPath, settingsBytes);

            var storage = CreateStorage(
                new TranslationConfiguration(),
                configurationPath,
                protector,
                new DpapiSecretStore(protector, Path.Combine(directory, "secrets")));

            Assert.False(storage.LoadConfiguration());
            Assert.False(storage.CanSaveConfiguration);
            storage.SaveConfiguration();
            Assert.Equal(settingsBytes, File.ReadAllBytes(configurationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static byte[] CreateProtectedSettings(string xmlDocument, IDataProtectionService protector)
    {
        var xml = Encoding.UTF8.GetBytes(xmlDocument);
        var protectedPayload = protector.Protect(xml, ConfigurationStorage.SETTINGS_PROTECTION_PURPOSE);
        var settingsBytes = new byte[5 + protectedPayload.Length];
        Buffer.BlockCopy(new byte[] { 0x54, 0x52, 0x53, 0x41, 1 }, 0, settingsBytes, 0, 5);
        Buffer.BlockCopy(protectedPayload, 0, settingsBytes, 5, protectedPayload.Length);
        return settingsBytes;
    }

    private static ConfigurationStorage CreateStorage(
        TranslationConfiguration configuration,
        string configurationPath,
        IDataProtectionService dataProtectionService,
        ISecretStore secretStore)
    {
        var storage = new ConfigurationStorage(
            new TestServiceProvider(configuration),
            dataProtectionService,
            secretStore,
            NullLogger<ConfigurationStorage>.Instance,
            configurationPath);
        storage.RegisterConfiguration<TranslationConfiguration>();
        return storage;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "TraduSaga.Security.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly IDictionary<Type, object> _services;

        public TestServiceProvider(TranslationConfiguration configuration)
        {
            _services = new Dictionary<Type, object>
            {
                [typeof(TranslationConfiguration)] = configuration
            };
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }
}
