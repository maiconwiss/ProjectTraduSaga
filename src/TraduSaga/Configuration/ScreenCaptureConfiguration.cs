using System;
using System.Drawing;
using Point = System.Windows.Point;
using TraduSaga.Utils;

namespace TraduSaga.Configuration
{
    public class ScreenCaptureConfiguration : BindableBase
    {
        private RectangleF _captureArea;

        public RectangleF CaptureArea
        {
            get => _captureArea;
            set => SetProperty(ref _captureArea, value);
        }
    }
}
