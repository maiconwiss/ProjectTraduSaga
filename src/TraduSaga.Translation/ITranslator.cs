using System.Threading.Tasks;

namespace TraduSaga.Translation
{
    public interface ITranslator
    {
        Task<string> TranslateTextAsync(string sourceText);
    }
}
