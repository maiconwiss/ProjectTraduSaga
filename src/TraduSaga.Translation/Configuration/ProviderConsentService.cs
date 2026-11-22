using System;
using System.Threading;
using System.Threading.Tasks;

namespace TraduSaga.Translation.Configuration
{
    public sealed class ProviderConsentService : IProviderConsentService
    {
        private readonly TranslationConfiguration _configuration;
        private readonly IProviderConsentPrompt _prompt;
        private readonly SemaphoreSlim _promptLock = new(1, 1);

        public ProviderConsentService(TranslationConfiguration configuration, IProviderConsentPrompt prompt)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        }

        public async Task<bool> EnsureGrantedAsync(Translators provider)
        {
            var state = GetState(provider);
            if (state != ProviderConsentState.Unknown)
            {
                return state == ProviderConsentState.Granted;
            }

            await _promptLock.WaitAsync().ConfigureAwait(false);
            try
            {
                state = GetState(provider);
                if (state != ProviderConsentState.Unknown)
                {
                    return state == ProviderConsentState.Granted;
                }

                var decision = await _prompt.PromptAsync(provider).ConfigureAwait(false);
                switch (decision)
                {
                    case ProviderConsentDecision.Allow:
                        SetState(provider, ProviderConsentState.Granted);
                        return true;
                    case ProviderConsentDecision.Deny:
                        SetState(provider, ProviderConsentState.Denied);
                        return false;
                    case ProviderConsentDecision.Cancel:
                        return false;
                    default:
                        throw new InvalidOperationException("The consent prompt returned an unsupported decision.");
                }
            }
            finally
            {
                _promptLock.Release();
            }
        }

        public ProviderConsentState GetState(Translators provider)
        {
            return provider switch
            {
                Translators.Google => _configuration.GoogleConsent,
                Translators.Deepl => _configuration.DeeplConsent,
                Translators.Yandex => _configuration.YandexConsent,
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
        }

        public void SetState(Translators provider, ProviderConsentState state)
        {
            if (!Enum.IsDefined(typeof(ProviderConsentState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state));
            }

            switch (provider)
            {
                case Translators.Google:
                    _configuration.GoogleConsent = state;
                    break;
                case Translators.Deepl:
                    _configuration.DeeplConsent = state;
                    break;
                case Translators.Yandex:
                    _configuration.YandexConsent = state;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider));
            }
        }
    }
}
