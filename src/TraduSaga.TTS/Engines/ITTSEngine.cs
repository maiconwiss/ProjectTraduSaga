namespace TraduSaga.TTS.Engines;

public interface ITTSEngine: IDisposable
{
    void SpeechText(string text);
}
