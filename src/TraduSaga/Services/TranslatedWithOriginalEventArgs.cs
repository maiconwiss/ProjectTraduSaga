using System;

namespace TraduSaga.Services
{
    public class TranslatedWithOriginalEventArgs : EventArgs
    {
        public string Original { get; set; }
        public string Translated { get; set; }
        public TraduSaga.Infrastructure.TextTypes TextType { get; set; }

        public TranslatedWithOriginalEventArgs(string original, string translated, TraduSaga.Infrastructure.TextTypes textType)
        {
            Original = original;
            Translated = translated;
            TextType = textType;
        }
    }
}
