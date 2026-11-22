using System;
using System.Drawing;

namespace TraduSaga.Processing.Interfaces
{
    public interface IScreenCapturer : IDisposable
    {
        int CaptureAttempts { get; set; }

        void Initialize();
        byte[] CaptureScreen();
        byte[] CaptureScreen(RectangleF captureArea);
    }
}
