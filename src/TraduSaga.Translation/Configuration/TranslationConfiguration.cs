using System.Xml.Serialization;
using TraduSaga.Infrastructure.Language;
using TraduSaga.Utils;

namespace TraduSaga.Translation.Configuration
{
    public class TranslationConfiguration : BindableBase
    {
        public static TranslationConfiguration Default => new TranslationConfiguration()
        {
            TranslateFromLang = Languages.English,
            TranslateToLang = Languages.Russian,
            Translator = Translators.Google,
            DeeplApiKey = string.Empty,
            YandexApiKey = string.Empty,
            GoogleConsent = ProviderConsentState.Unknown,
            DeeplConsent = ProviderConsentState.Unknown,
            YandexConsent = ProviderConsentState.Unknown
        };

        public Languages TranslateFromLang
        {
            get => _translateFromLang;
            set
            {
                SetProperty(ref _translateFromLang, value);
            }
        }

        public Languages TranslateToLang
        {
            get => _translateToLang;
            set
            {
                SetProperty(ref _translateToLang, value);
            }
        }

        public Translators Translator
        {
            get => _translator;
            set
            {
                SetProperty(ref _translator, value);
            }
        }

        public ProviderConsentState GoogleConsent
        {
            get => _googleConsent;
            set => SetProperty(ref _googleConsent, value);
        }

        public ProviderConsentState DeeplConsent
        {
            get => _deeplConsent;
            set => SetProperty(ref _deeplConsent, value);
        }

        public ProviderConsentState YandexConsent
        {
            get => _yandexConsent;
            set => SetProperty(ref _yandexConsent, value);
        }

        [XmlIgnore]
        public string DeeplApiKey
        {
            get => _deeplApiKey;
            set
            {
                SetProperty(ref _deeplApiKey, value);
            }
        }

        [XmlIgnore]
        public string YandexApiKey
        {
            get => _yandexApiKey;
            set
            {
                SetProperty(ref _yandexApiKey, value);
            }
        }


        private Languages _translateFromLang;
        private Languages _translateToLang;
        private Translators _translator;
        private ProviderConsentState _googleConsent;
        private ProviderConsentState _deeplConsent;
        private ProviderConsentState _yandexConsent;
        private string _deeplApiKey = string.Empty;
        private string _yandexApiKey = string.Empty;
    }
}
