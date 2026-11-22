using TraduSaga.Infrastructure.Language;

namespace TraduSaga.OCR
{
    public interface IOCREngine
    {
        byte PrimaryPriority { get; }
        bool SecondaryPrimaryCheck { get; }
        int Confidence { get; }
        Languages DetectionLanguage { get; }

        string[] GetTextLines(byte[] image);
    }
}
