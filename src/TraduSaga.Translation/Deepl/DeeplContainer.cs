using TraduSaga.Translation.Configuration;
using TraduSaga.Utils.Http;

namespace TraduSaga.Translation.Deepl
{
    public sealed class DeeplContainer : TranslationContainer
    {
        public HttpReader Reader { get; private set; }

        public DeeplContainer(bool isPrimary = false) : base(isPrimary)
        {
            Reader = CreateReader();
        }

        private HttpReader CreateReader()
        {
            var deeplReader = new HttpReader();
            deeplReader.ThrowExceptions = false;
            deeplReader.ContentType = "application/json";
            deeplReader.Accept = "*/*";
            deeplReader.UserAgent = "TraduSaga";

            return deeplReader;
        }
    }
}
