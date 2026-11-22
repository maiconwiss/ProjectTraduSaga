using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Input;
using OpenCvSharp;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Input;
using TraduSaga.Dialog;
using TraduSaga.Dialog.Stages;
using TraduSaga.Infrastructure.Language;
using TraduSaga.MVVM.Common;
using TraduSaga.MVVM.Models;
using TraduSaga.OCR.Configuration;
using TraduSaga.OCR.WindowsOCR;
using TraduSaga.Translation;
using TraduSaga.Translation.Configuration;
using TraduSaga.TTS;
using TraduSaga.Utils;
using TraduSaga.Utils.Extensions;
using RelayCommand = Microsoft.Toolkit.Mvvm.Input.RelayCommand;

namespace TraduSaga.MVVM.ViewModels
{
    public sealed class LanguagesSettingsViewModel : BindableBase, IAdditionalPanelController, IDisposable
    {
        public event EventHandler<bool> PanelStateIsChanged;


        public IList<DisplayLanguage> AvailableLanguages { get; set; }
        public IList<DisplayLanguage> AvailableTranslationLanguages { get; set; }

        public TranslationConfiguration Model { get; set; }

        public TtsConfiguration TtsSettings { get; set; }

        private ObservableCollection<VoiceInfo> _availableVoices;
        public ObservableCollection<VoiceInfo> AvailableVoices
        {
            get => _availableVoices;
            set => SetProperty(ref _availableVoices, value);
        }

        private VoiceInfo _selectedVoice;
        public VoiceInfo SelectedVoice
        {
            get => _selectedVoice;
            set
            {
                SetProperty(ref _selectedVoice, value);
                if (value != null)
                {
                    Action updateVoiceAction = () =>
                    {
                        TtsSettings.SelectedVoiceName = value.Name;
                    };
                    
                    _ = ReconfigureTts(TtsSettings.TtsLanguage, TtsSettings.TtsSystem, updateVoiceAction);
                }
            }
        }

        public bool IsTtsWindowsSelected => TtsSettings.TtsSystem == TTSEngines.WindowsTTS;

        public bool IsDeeplSelected => Model.Translator == Translators.Deepl;

        public bool IsYandexSelected => Model.Translator == Translators.Yandex;

        public Translators Translator
        {
            get => Model.Translator;
            set
            {
                Model.Translator = value;
                HideApiKeys();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDeeplSelected));
                OnPropertyChanged(nameof(IsYandexSelected));
            }
        }

        public string DeeplApiKey
        {
            get => Model.DeeplApiKey;
            set
            {
                Model.DeeplApiKey = value;
                OnPropertyChanged(nameof(DeeplApiKey));
                OnPropertyChanged(nameof(DeeplApiKeyStatus));
                OnPropertyChanged(nameof(IsDeeplApiKeyConfigured));
            }
        }

        public string DeeplApiKeyStatus => GetApiKeyStatus(DeeplApiKey);

        public bool IsDeeplApiKeyConfigured => !string.IsNullOrEmpty(DeeplApiKey);

        public bool IsDeeplApiKeyRevealed { get; private set; }

        public string DeeplApiKeyRevealAction => GetApiKeyRevealAction(IsDeeplApiKeyRevealed);

        public string YandexApiKey
        {
            get => Model.YandexApiKey;
            set
            {
                Model.YandexApiKey = value;
                OnPropertyChanged(nameof(YandexApiKey));
                OnPropertyChanged(nameof(YandexApiKeyStatus));
                OnPropertyChanged(nameof(IsYandexApiKeyConfigured));
            }
        }

        public string YandexApiKeyStatus => GetApiKeyStatus(YandexApiKey);

        public bool IsYandexApiKeyConfigured => !string.IsNullOrEmpty(YandexApiKey);

        public bool IsYandexApiKeyRevealed { get; private set; }

        public string YandexApiKeyRevealAction => GetApiKeyRevealAction(IsYandexApiKeyRevealed);

        public bool IsTtsEnabled => TtsSettings.TtsSystem != TTSEngines.None;

        public void SetDeeplApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("An API key is required.", nameof(apiKey));
            }

            DeeplApiKey = apiKey;
            IsDeeplApiKeyRevealed = false;
            OnPropertyChanged(nameof(IsDeeplApiKeyRevealed));
            OnPropertyChanged(nameof(DeeplApiKeyRevealAction));
        }

        public void DeleteDeeplApiKey()
        {
            DeeplApiKey = string.Empty;
            IsDeeplApiKeyRevealed = false;
            OnPropertyChanged(nameof(IsDeeplApiKeyRevealed));
            OnPropertyChanged(nameof(DeeplApiKeyRevealAction));
        }

        public void SetYandexApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("An API key is required.", nameof(apiKey));
            }

            YandexApiKey = apiKey;
            IsYandexApiKeyRevealed = false;
            OnPropertyChanged(nameof(IsYandexApiKeyRevealed));
            OnPropertyChanged(nameof(YandexApiKeyRevealAction));
        }

        public void DeleteYandexApiKey()
        {
            YandexApiKey = string.Empty;
            IsYandexApiKeyRevealed = false;
            OnPropertyChanged(nameof(IsYandexApiKeyRevealed));
            OnPropertyChanged(nameof(YandexApiKeyRevealAction));
        }

        public void ToggleDeeplApiKeyReveal()
        {
            IsDeeplApiKeyRevealed = !IsDeeplApiKeyRevealed && !string.IsNullOrEmpty(DeeplApiKey);
            OnPropertyChanged(nameof(IsDeeplApiKeyRevealed));
            OnPropertyChanged(nameof(DeeplApiKeyRevealAction));
        }

        public void ToggleYandexApiKeyReveal()
        {
            IsYandexApiKeyRevealed = !IsYandexApiKeyRevealed && !string.IsNullOrEmpty(YandexApiKey);
            OnPropertyChanged(nameof(IsYandexApiKeyRevealed));
            OnPropertyChanged(nameof(YandexApiKeyRevealAction));
        }

        public void HideApiKeys()
        {
            IsDeeplApiKeyRevealed = false;
            IsYandexApiKeyRevealed = false;
            OnPropertyChanged(nameof(IsDeeplApiKeyRevealed));
            OnPropertyChanged(nameof(DeeplApiKeyRevealAction));
            OnPropertyChanged(nameof(IsYandexApiKeyRevealed));
            OnPropertyChanged(nameof(YandexApiKeyRevealAction));
        }



        public Languages TranslateFromLang
        {
            get => Model.TranslateFromLang;
            set
            {
                ChangeSourceLanguage(value);
            }
        }

        public Languages TranslateToLang
        {
            get => Model.TranslateToLang;
            set
            {
                ChangeTargetLanguage(value);
            }
        }

        public TTSEngines TtsSystem
        {
            get => TtsSettings.TtsSystem;
            set
            {
                ChangeTtsSystem(value);
            }
        }



        private readonly DialogService _dialogService;
        private readonly OcrGeneralConfiguration _ocrConfiguration;
        private readonly LanguageService _languageService;
        private readonly ILogger _logger;
        private readonly IProviderConsentService _providerConsentService;

        public bool IsGoogleConsentGranted
        {
            get => _providerConsentService.GetState(Translators.Google) == ProviderConsentState.Granted;
            set => SetProviderConsent(Translators.Google, value ? ProviderConsentState.Granted : ProviderConsentState.Denied);
        }

        public bool IsDeeplConsentGranted
        {
            get => _providerConsentService.GetState(Translators.Deepl) == ProviderConsentState.Granted;
            set => SetProviderConsent(Translators.Deepl, value ? ProviderConsentState.Granted : ProviderConsentState.Denied);
        }

        public bool IsYandexConsentGranted
        {
            get => _providerConsentService.GetState(Translators.Yandex) == ProviderConsentState.Granted;
            set => SetProviderConsent(Translators.Yandex, value ? ProviderConsentState.Granted : ProviderConsentState.Denied);
        }

        public string GoogleConsentStatus => GetConsentStatus(Translators.Google);
        public string DeeplConsentStatus => GetConsentStatus(Translators.Deepl);
        public string YandexConsentStatus => GetConsentStatus(Translators.Yandex);

        public LanguagesSettingsViewModel(LanguageService languageService, TranslationConfiguration translationConfiguration,
            OcrGeneralConfiguration ocrConfiguration, TtsConfiguration ttsConfiguration, DialogService dialogService,
            ILogger<LanguagesSettingsViewModel> logger, IProviderConsentService providerConsentService)
        {
            var languages = languageService.GetAll(true)
                .Select(lang => (lang.TranslationOnly, new DisplayLanguage(lang, GetLanguageDisplayName(lang))))
                .ToArray();
            this.AvailableLanguages = languages
                .Where(lang => !lang.TranslationOnly)
                .OrderBy(lang => lang.Item2.DisplayName)
                .Select(lang => lang.Item2)
                .ToList();

            this.AvailableTranslationLanguages = languages
                .OrderBy(lang => lang.Item2.DisplayName)
                .Select(lang => lang.Item2)
                .ToList();

            this.Model = translationConfiguration;
            this.TtsSettings = ttsConfiguration;
            this.TtsSettings.TtsLanguage = this.Model.TranslateToLang;

            this.AvailableVoices = new ObservableCollection<VoiceInfo>();
            
            if (this.TtsSettings.TtsSystem == TTSEngines.WindowsTTS)
            {
                var languageCode = languageService.GetLanguageDescriptor(this.TtsSettings.TtsLanguage).Code;
                LoadAvailableVoices(languageCode);
            }

            this._languageService = languageService;
            this._dialogService = dialogService;
            this._ocrConfiguration = ocrConfiguration;
            this._logger = logger;
            this._providerConsentService = providerConsentService;
        }

        public void ForgetGoogleConsent()
        {
            SetProviderConsent(Translators.Google, ProviderConsentState.Unknown);
        }

        public void ForgetDeeplConsent()
        {
            SetProviderConsent(Translators.Deepl, ProviderConsentState.Unknown);
        }

        public void ForgetYandexConsent()
        {
            SetProviderConsent(Translators.Yandex, ProviderConsentState.Unknown);
        }

        private void SetProviderConsent(Translators provider, ProviderConsentState state)
        {
            _providerConsentService.SetState(provider, state);
            OnPropertyChanged(nameof(IsGoogleConsentGranted));
            OnPropertyChanged(nameof(IsDeeplConsentGranted));
            OnPropertyChanged(nameof(IsYandexConsentGranted));
            OnPropertyChanged(nameof(GoogleConsentStatus));
            OnPropertyChanged(nameof(DeeplConsentStatus));
            OnPropertyChanged(nameof(YandexConsentStatus));
        }

        private string GetConsentStatus(Translators provider)
        {
            var resourceKey = _providerConsentService.GetState(provider) switch
            {
                ProviderConsentState.Unknown => "Str.LangSettings.ConsentUnknown",
                ProviderConsentState.Denied => "Str.LangSettings.ConsentDenied",
                ProviderConsentState.Granted => "Str.LangSettings.ConsentGranted",
                _ => throw new InvalidOperationException("The provider consent state is invalid.")
            };

            return LocalizationManager.GetValue(resourceKey, true);
        }

        private void LoadAvailableVoices(string languageCode)
        {
            try
            {
                var voices = GetAvailableVoicesForLanguage(languageCode);
                AvailableVoices = new ObservableCollection<VoiceInfo>(voices);
                
                if (!string.IsNullOrEmpty(TtsSettings.SelectedVoiceName))
                {
                    _selectedVoice = AvailableVoices.FirstOrDefault(v => 
                        v.Name.Equals(TtsSettings.SelectedVoiceName, StringComparison.OrdinalIgnoreCase));
                }
                
                if (_selectedVoice == null && AvailableVoices.Count > 0)
                {
                    _selectedVoice = AvailableVoices[0];
                    TtsSettings.SelectedVoiceName = _selectedVoice.Name;
                }
                
                OnPropertyChanged(nameof(SelectedVoice));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load available voices error");
                AvailableVoices = new ObservableCollection<VoiceInfo>();
            }
        }

        private List<VoiceInfo> GetAvailableVoicesForLanguage(string languageTag)
        {
            using var synth = new SpeechSynthesizer();
            var result = new List<VoiceInfo>();
            
            try
            {
                var voices = synth.GetInstalledVoices(new CultureInfo(languageTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                    return result;
                }
            }
            catch
            {
            }

            try
            {
                var shortTag = languageTag.Split('-')[0];
                var voices = synth.GetInstalledVoices(new CultureInfo(shortTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                }
            }
            catch
            {
            }

            return result;
        }


        private async Task ChangeSourceLanguage(Languages language)
        {
            try
            {
                var changeLangStage = StagesFactory.CreateLanguageChangeStages(_dialogService, () => Model.TranslateFromLang = language,
                    _logger);

                if (_ocrConfiguration.GetConfiguration<WindowsOCRConfiguration>().Enabled)
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    changeLangStage = StagesFactory.CreateWindowsOcrCheckingStages(_dialogService, langCode, changeLangStage, _logger);
                }

                await changeLangStage.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during source language change");
            }

            OnPropertyChanged(nameof(TranslateFromLang));
        }

        private async Task ChangeTargetLanguage(Languages language)
        {
            var changeLanguageAction = () =>
            {
                this.TtsSettings.TtsLanguage = language;
                this.Model.TranslateToLang = language;
                
                if (TtsSettings.TtsSystem == TTSEngines.WindowsTTS)
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    LoadAvailableVoices(langCode);
                }
            };

            await this.ReconfigureTts(language, TtsSettings.TtsSystem, changeLanguageAction);
            OnPropertyChanged(nameof(TranslateToLang));
            OnPropertyChanged(nameof(IsTtsWindowsSelected));
            OnPropertyChanged(nameof(IsTtsEnabled));
        }

        private async Task ChangeTtsSystem(TTSEngines engine)
        {
            Action changeTtsEngineAction = () => 
            {
                this.TtsSettings.TtsSystem = engine;
                
                if (engine == TTSEngines.WindowsTTS)
                {
                    var langCode = _languageService.GetLanguageDescriptor(TtsSettings.TtsLanguage).Code;
                    LoadAvailableVoices(langCode);
                }
                else
                {
                    AvailableVoices = new ObservableCollection<VoiceInfo>();
                    _selectedVoice = null;
                    OnPropertyChanged(nameof(SelectedVoice));
                }
            };
            
            await this.ReconfigureTts(TtsSettings.TtsLanguage, engine, changeTtsEngineAction);
            OnPropertyChanged(nameof(TtsSystem));
            OnPropertyChanged(nameof(IsTtsWindowsSelected));
            OnPropertyChanged(nameof(IsTtsEnabled));
        }

        private async Task ReconfigureTts(Languages language, TTSEngines engine, Action changeParameter)
        {
            try
            {
                var changeLangStage = StagesFactory.CreateLanguageChangeStages(
                    _dialogService,
                    changeParameter,
                    _logger);

                if (engine == TTSEngines.WindowsTTS
                    && !TtsSettings.InstalledWinTtsLanguages.Contains(language))
                {
                    var langCode = _languageService.GetLanguageDescriptor(language).Code;
                    changeLangStage.AddNextStage(new ActionInteractionStage(_dialogService, () =>
                    {
                        this.TtsSettings.InstalledWinTtsLanguages.Add(language);
                        return Task.CompletedTask;
                    }));
                    changeLangStage = StagesFactory.CreateWindowsTtsCheckingStages(_dialogService, langCode, changeLangStage, _logger);
                }
                //else if (engine == TTSEngines.SileroTTS)
                //{
                //    var languageDescriptor = _languageService.GetLanguageDescriptor(language);
                //    changeLangStage = StagesFactory.CreateSileroTtsCheckingStages(languageDescriptor, _dialogService, changeLangStage, _logger);
                //}

                await changeLangStage.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during source language change");
            }
        }

        private string GetLanguageDisplayName(LanguageDescriptor languageDescriptor)
        {
            return LocalizationManager.GetValue($"Str.Languages.{languageDescriptor.Language}", false,
               OnLocalizedValueChanged, this);
        }

        private static string GetApiKeyStatus(string apiKey)
        {
            return LocalizationManager.GetValue(string.IsNullOrEmpty(apiKey)
                ? "Str.LangSettings.ApiKeyNotConfigured"
                : "Str.LangSettings.ApiKeyConfigured", true);
        }

        private static string GetApiKeyRevealAction(bool isRevealed)
        {
            return LocalizationManager.GetValue(isRevealed
                ? "Str.LangSettings.HideApiKey"
                : "Str.LangSettings.RevealApiKey", true);
        }

        private void OnLocalizedValueChanged(string key, string oldValue)
        {
            var availableLang = AvailableTranslationLanguages.First(lang => lang.DisplayName == oldValue);
            availableLang.DisplayName = LocalizationManager.GetValue(key, false, OnLocalizedValueChanged, this);
        }


        public void ClosePanel()
        {
        }

        public void Dispose()
        {
            HideApiKeys();
            LocalizationManager.ReleaseChangedValuesCallbacks(this);
        }
    }
}