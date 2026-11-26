using System.Windows.Media;

namespace TraduSaga.Services
{
    internal sealed class ScreenColorPickerState
    {
        public bool IsDragging { get; private set; }
        public bool SamplingFailed { get; private set; }
        public Color? PreviewColor { get; private set; }
        public Color? SelectedColor { get; private set; }

        public void BeginDrag()
        {
            IsDragging = true;
        }

        public bool Preview(Color color)
        {
            if (SamplingFailed)
            {
                return false;
            }

            PreviewColor = color;
            return true;
        }

        public bool Commit()
        {
            if (!IsDragging || SamplingFailed || !PreviewColor.HasValue)
            {
                return false;
            }

            IsDragging = false;
            SelectedColor = PreviewColor;
            return true;
        }

        public void Cancel()
        {
            IsDragging = false;
            PreviewColor = null;
            SelectedColor = null;
        }

        public void Fail()
        {
            IsDragging = false;
            PreviewColor = null;
            SelectedColor = null;
            SamplingFailed = true;
        }
    }

    internal sealed class ScreenColorSelectionDraft
    {
        private readonly Color _originalColor;
        private readonly string _originalHexText;
        private readonly bool _originalIsValid;

        public Color Color { get; private set; }
        public string HexText { get; private set; }
        public bool IsValid { get; private set; }

        public ScreenColorSelectionDraft(Color color, string hexText, bool isValid)
        {
            _originalColor = color;
            _originalHexText = hexText;
            _originalIsValid = isValid;
            Color = color;
            HexText = hexText;
            IsValid = isValid;
        }

        public void Preview(Color color)
        {
            Color = color;
            HexText = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            IsValid = true;
        }

        public void Cancel()
        {
            Color = _originalColor;
            HexText = _originalHexText;
            IsValid = _originalIsValid;
        }
    }
}
