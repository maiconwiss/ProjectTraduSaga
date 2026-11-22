using System.Threading.Tasks;

namespace TraduSaga.Translation.Configuration
{
    public interface IProviderConsentService
    {
        Task<bool> EnsureGrantedAsync(Translators provider);

        ProviderConsentState GetState(Translators provider);

        void SetState(Translators provider, ProviderConsentState state);
    }
}
