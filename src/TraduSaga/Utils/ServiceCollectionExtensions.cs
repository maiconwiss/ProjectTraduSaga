using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraduSaga.Configuration;
using TraduSaga.HotKeys;
using TraduSaga.Infrastructure.Encryption;
using TraduSaga.OCR.Configuration;
using TraduSaga.Translation.Configuration;
using TraduSaga.TTS;

namespace TraduSaga.Utils
{
    public static class ServiceCollectionExtensions
    {
        public static void AddConfigurationStorage(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ConfigurationStorage>(sc =>
            {
                var confStorage = new ConfigurationStorage(
                    sc,
                    sc.GetService<IDataProtectionService>(),
                    sc.GetService<ISecretStore>(),
                    sc.GetService<ILogger<ConfigurationStorage>>());
                confStorage.RegisterConfiguration<ChatWindowConfiguration>();
                confStorage.RegisterConfiguration<ScreenCaptureConfiguration>();
                confStorage.RegisterConfiguration<TranslationWindowGeometry>();
                confStorage.RegisterConfiguration<OcrGeneralConfiguration>();
                confStorage.RegisterConfiguration<TranslationConfiguration>();
                confStorage.RegisterConfiguration<TtsConfiguration>();
                confStorage.RegisterConfiguration<SystemConfiguration>();
                confStorage.RegisterConfiguration<HotKeysConfiguration>();

                return confStorage;
            });
        }
    }
}
