using System;
using System.Drawing;
using Point = System.Windows.Point;
using TraduSaga.Utils;
using TraduSaga.Processing.Configuration;

namespace TraduSaga.Configuration
{
    public class ScreenCaptureConfiguration : BindableBase, ISpeakerAreaConfiguration
    {
        private RectangleF _captureArea;
        private RectangleF _speakerArea;

        public RectangleF CaptureArea
        {
            get => _captureArea;
            set => SetProperty(ref _captureArea, value);
        }

        public RectangleF SpeakerArea
        {
            get => _speakerArea;
            set => SetProperty(ref _speakerArea, value);
        }
    }
}
