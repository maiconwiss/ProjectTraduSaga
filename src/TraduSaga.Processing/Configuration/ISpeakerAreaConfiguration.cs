using System.Drawing;

namespace TraduSaga.Processing.Configuration
{
    public interface ISpeakerAreaConfiguration
    {
        RectangleF CaptureArea { get; }
        RectangleF SpeakerArea { get; }
    }
}
