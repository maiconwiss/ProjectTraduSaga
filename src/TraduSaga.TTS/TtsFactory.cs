using TraduSaga.Infrastructure.Language;
using TraduSaga.TTS.Engines;

namespace TraduSaga.TTS
{
    public class TtsFactory
    {
        private readonly LanguageService _languageService;

        public TtsFactory(LanguageService languageService)
        {
            _languageService = languageService;
        }

        public ITTSEngine CreateTtsEngine(TtsConfiguration ttsConfiguration) =>
            ttsConfiguration.TtsSystem switch
            {
                TTSEngines.None => new NoneTTSEngine(),
                TTSEngines.WindowsTTS => new WindowsTTSEngine(
                    GetLangCode(ttsConfiguration), 
                    ttsConfiguration.SelectedVoiceName),
                _ => throw new NotSupportedException()
            };

        private string GetLangCode(TtsConfiguration ttsConfiguration) =>
            _languageService.GetLanguageDescriptor(ttsConfiguration.TtsLanguage).Code;
    }
}