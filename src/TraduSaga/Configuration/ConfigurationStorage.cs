using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using TraduSaga.Infrastructure.Encryption;
using TraduSaga.Translation.Configuration;
using TraduSaga.Utils.Extensions;

namespace TraduSaga.Configuration
{
    public class  ConfigurationStorage
    {
        private const int SAVE_DELAY_MS = 700;
        private const byte SETTINGS_FORMAT_VERSION = 1;
        internal const string SETTINGS_PROTECTION_PURPOSE = "TraduSaga.Settings.v1";
        private static readonly byte[] SETTINGS_MAGIC = { 0x54, 0x52, 0x53, 0x41 };

        private readonly IServiceProvider _serviceProvider;
        private readonly IDataProtectionService _dataProtectionService;
        private readonly ISecretStore _secretStore;
        private readonly IList<Type> _configurationTypes;
        private readonly ILogger _logger;
        private readonly string _configurationPath;
        private readonly string _legacyConfigurationPath;
        private readonly object _saveLock = new object();
        private readonly Timer _saveTimer;
        private bool _canSaveConfiguration = true;

        public ConfigurationStorage(
            IServiceProvider serviceProvider,
            IDataProtectionService dataProtectionService,
            ISecretStore secretStore,
            ILogger<ConfigurationStorage> logger,
            string configurationPath = null)
        {
            this._logger = logger;
            _serviceProvider = serviceProvider;
            _dataProtectionService = dataProtectionService;
            _secretStore = secretStore;
            _configurationTypes = new List<Type>();
            _configurationPath = configurationPath ?? GetDefaultConfigurationPath(Environment.SpecialFolder.LocalApplicationData);
            _legacyConfigurationPath = configurationPath == null
                ? GetDefaultConfigurationPath(Environment.SpecialFolder.ApplicationData)
                : null;
            _saveTimer = new Timer(_ => SaveConfiguration(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public bool CanSaveConfiguration => _canSaveConfiguration;

        public void RegisterConfiguration<TConfiguration>()
            where TConfiguration: class
        {
            if (!_configurationTypes.Contains(typeof(TConfiguration)))
            {
                _configurationTypes.Add(typeof(TConfiguration));
            }
        }

        public bool LoadConfiguration()
        {
            List<object> configurations = _configurationTypes.Select(type => _serviceProvider.GetService(type)).ToList();
            var serializer = new XmlSerializer(typeof(List<object>), _configurationTypes.ToArray());
            try
            {
                SecureFileStorage.EnsurePrivateDirectory(Path.GetDirectoryName(_configurationPath));
                if (!File.Exists(_configurationPath) && _legacyConfigurationPath != null && File.Exists(_legacyConfigurationPath))
                {
                    throw new InvalidDataException("A legacy settings file was found and automatic migration is disabled.");
                }

                if (File.Exists(_configurationPath))
                {
                    _logger.LogTrace("Loading protected configuration");
                    var protectedSettings = File.ReadAllBytes(_configurationPath);
                    var serializedSettings = UnprotectSettings(protectedSettings);
                    ValidateRegisteredConfigurationTypes(serializedSettings);
                    List<object> savedConfigs;
                    using (var settingsStream = new MemoryStream(serializedSettings, writable: false))
                    {
                        using var settingsReader = XmlReader.Create(settingsStream, new XmlReaderSettings
                        {
                            DtdProcessing = DtdProcessing.Prohibit,
                            XmlResolver = null
                        });
                        savedConfigs = serializer.Deserialize(settingsReader) as List<object>;
                    }

                    if (savedConfigs == null)
                    {
                        throw new InvalidDataException("The settings payload has an invalid shape.");
                    }

                    foreach (var configuration in configurations)
                    {
                        var savedConfig = savedConfigs.FirstOrDefault(dc => dc.GetType() == configuration.GetType());
                        if (savedConfig != null)
                        {
                            savedConfig.MapTo(configuration);
                        }
                    }
                }

                LoadSecrets(configurations);
                SubscribeToChanges(configurations);
                _logger.LogTrace("Configuration loaded");
                return true;
            }
            catch (Exception ex)
            {
                _canSaveConfiguration = false;
                SubscribeToChanges(configurations);
                _logger.LogError(ex, "Configuration could not be loaded; the original settings file was preserved");
                return false;
            }
        }

        private void LoadSecrets(IEnumerable<object> configurations)
        {
            var translationConfiguration = configurations.OfType<TranslationConfiguration>().SingleOrDefault();
            if (translationConfiguration == null)
            {
                return;
            }

            translationConfiguration.DeeplApiKey = _secretStore.GetSecret(DpapiSecretStore.DeepLApiKeyName) ?? string.Empty;
            translationConfiguration.YandexApiKey = _secretStore.GetSecret(DpapiSecretStore.YandexApiKeyName) ?? string.Empty;
        }

        private void SubscribeToChanges(IEnumerable<object> configurations)
        {
            foreach (var configuration in configurations)
            {
                Subscribe(configuration);
                foreach (var property in configuration.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        Subscribe(property.GetValue(configuration));
                    }
                }
            }
        }

        private void Subscribe(object target)
        {
            if (target is INotifyPropertyChanged notifyingTarget)
            {
                notifyingTarget.PropertyChanged -= ConfigurationOnPropertyChanged;
                notifyingTarget.PropertyChanged += ConfigurationOnPropertyChanged;
            }
        }

        private void ConfigurationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _saveTimer.Change(SAVE_DELAY_MS, Timeout.Infinite);
        }

        public void SaveConfiguration()
        {
            lock (_saveLock)
            {
                if (!_canSaveConfiguration)
                {
                    _logger.LogWarning("Configuration save skipped because the existing settings file could not be loaded");
                    return;
                }

                List<object> configurations = _configurationTypes.Select(type => _serviceProvider.GetService(type)).ToList();

                var serializer = new XmlSerializer(typeof(List<object>), _configurationTypes.ToArray());
                try
                {
                    SaveSecrets(configurations);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        serializer.Serialize(ms, configurations);
                        byte[] protectedSettings = ProtectSettings(ms.ToArray());
                        SecureFileStorage.WriteAtomically(_configurationPath, protectedSettings);
                    }

                    _logger.LogTrace("Configuration saved");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save configuration");
                }
            }
        }

        private void SaveSecrets(IEnumerable<object> configurations)
        {
            var translationConfiguration = configurations.OfType<TranslationConfiguration>().SingleOrDefault();
            if (translationConfiguration == null)
            {
                return;
            }

            _secretStore.SetSecret(DpapiSecretStore.DeepLApiKeyName, translationConfiguration.DeeplApiKey ?? string.Empty);
            _secretStore.SetSecret(DpapiSecretStore.YandexApiKeyName, translationConfiguration.YandexApiKey ?? string.Empty);
        }

        private byte[] ProtectSettings(byte[] serializedSettings)
        {
            var protectedPayload = _dataProtectionService.Protect(serializedSettings, SETTINGS_PROTECTION_PURPOSE);
            var result = new byte[SETTINGS_MAGIC.Length + 1 + protectedPayload.Length];
            Buffer.BlockCopy(SETTINGS_MAGIC, 0, result, 0, SETTINGS_MAGIC.Length);
            result[SETTINGS_MAGIC.Length] = SETTINGS_FORMAT_VERSION;
            Buffer.BlockCopy(protectedPayload, 0, result, SETTINGS_MAGIC.Length + 1, protectedPayload.Length);
            return result;
        }

        private byte[] UnprotectSettings(byte[] protectedSettings)
        {
            if (protectedSettings.Length < SETTINGS_MAGIC.Length + 1
                || !protectedSettings.Take(SETTINGS_MAGIC.Length).SequenceEqual(SETTINGS_MAGIC)
                || protectedSettings[SETTINGS_MAGIC.Length] != SETTINGS_FORMAT_VERSION)
            {
                throw new InvalidDataException("The settings file format is unsupported.");
            }

            var protectedPayload = new byte[protectedSettings.Length - SETTINGS_MAGIC.Length - 1];
            Buffer.BlockCopy(protectedSettings, SETTINGS_MAGIC.Length + 1, protectedPayload, 0, protectedPayload.Length);
            return _dataProtectionService.Unprotect(protectedPayload, SETTINGS_PROTECTION_PURPOSE);
        }

        private void ValidateRegisteredConfigurationTypes(byte[] serializedSettings)
        {
            using var settingsStream = new MemoryStream(serializedSettings, writable: false);
            using var settingsReader = XmlReader.Create(settingsStream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            settingsReader.MoveToContent();
            var rootDepth = settingsReader.Depth;
            while (settingsReader.Read())
            {
                if (settingsReader.NodeType != XmlNodeType.Element
                    || settingsReader.Depth != rootDepth + 1)
                {
                    continue;
                }

                var xmlTypeName = settingsReader.GetAttribute("type", XmlSchema.InstanceNamespace);
                var typeNameParts = xmlTypeName?.Split(':');
                if (typeNameParts == null
                    || typeNameParts.Length < 1
                    || typeNameParts.Length > 2
                    || typeNameParts.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidDataException("The settings XML contains an unregistered configuration type.");
                }

                var prefix = typeNameParts.Length == 2 ? typeNameParts[0] : string.Empty;
                var namespaceUri = settingsReader.LookupNamespace(prefix);
                var localTypeName = typeNameParts[typeNameParts.Length - 1];
                var isRegisteredType = _configurationTypes.Any(type =>
                {
                    var xmlType = type.GetCustomAttribute<XmlTypeAttribute>();
                    var registeredName = string.IsNullOrEmpty(xmlType?.TypeName) ? type.Name : xmlType.TypeName;
                    var registeredNamespace = xmlType?.Namespace ?? string.Empty;
                    return string.Equals(registeredName, localTypeName, StringComparison.Ordinal)
                        && string.Equals(registeredNamespace, namespaceUri, StringComparison.Ordinal);
                });
                if (!isRegisteredType)
                {
                    throw new InvalidDataException("The settings XML contains an unregistered configuration type.");
                }
            }
        }

        internal static string GetDefaultConfigurationPath(Environment.SpecialFolder specialFolder)
        {
            string appDataDirectory = Environment.GetFolderPath(specialFolder);
            const string appName = "TraduSaga";
            string appDirectory = Path.Combine(appDataDirectory, appName);

            return Path.Combine(appDirectory, "settings");
        }
    }
}
