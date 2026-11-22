using System.Threading.Tasks;

namespace TraduSaga.Translation.Configuration
{
    public interface IProviderConsentPrompt
    {
        Task<ProviderConsentDecision> PromptAsync(Translators provider);
    }
}
