using TraduSaga.Utils;

namespace TraduSaga.Configuration
{
    public class TranslationWindowGeometry : BindableBase
    {
        private double _left;
        private double _top;
        private double _width = 550;
        private double _height = 250;

        public double Left
        {
            get => _left;
            set => SetProperty(ref _left, value);
        }

        public double Top
        {
            get => _top;
            set => SetProperty(ref _top, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }
    }
}
