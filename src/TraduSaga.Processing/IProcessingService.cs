using System.Drawing;

namespace TraduSaga.Processing
{
    public interface IProcessingService
    {
        bool IsStarted { get; }

        void StartProcessing();

        void ProcessOnce(RectangleF captureArea);

        void StopProcessing();
    }
}
