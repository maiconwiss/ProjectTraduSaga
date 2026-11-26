using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TraduSaga.Services;

namespace TraduSaga.MVVM.Views
{
    public partial class ScreenColorPickerWindow : Window
    {
        private readonly WindowsScreenColorSampler _colorSampler;
        private readonly ScreenColorPickerState _pickerState = new();
        private Cursor _previousCursor;

        public Color? SelectedColor => _pickerState.SelectedColor;
        public bool SamplingFailed => _pickerState.SamplingFailed;
        public event Action<Color> SampledColorChanged;

        public ScreenColorPickerWindow()
        {
            InitializeComponent();
            _colorSampler = new WindowsScreenColorSampler();
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Cross;
            if (!_colorSampler.IsAvailable)
            {
                _pickerState.Fail();
                Close();
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Mouse.OverrideCursor = _previousCursor;
            ReleaseMouseCapture();
            _colorSampler.Dispose();
        }

        private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _pickerState.BeginDrag();
            CaptureMouse();
            if (!SampleCursor())
            {
                Close();
            }

            e.Handled = true;
        }

        private void Window_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_pickerState.IsDragging)
            {
                if (!SampleCursor())
                {
                    Close();
                }

                return;
            }

            if (GetCursorPos(out var cursorPosition) &&
                _colorSampler.TrySample(cursorPosition.X, cursorPosition.Y, out var color) &&
                _pickerState.Preview(color))
            {
                UpdateSamplePreview(color, cursorPosition);
            }
            else
            {
                SamplePreview.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_pickerState.IsDragging)
            {
                return;
            }

            var sampleSucceeded = SampleCursor();
            ReleaseMouseCapture();
            if (sampleSucceeded && _pickerState.Commit())
            {
                Close();
            }
            else
            {
                _pickerState.Fail();
                Close();
            }

            e.Handled = true;
        }

        private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _pickerState.Cancel();
                Close();
                e.Handled = true;
            }
        }

        private bool SampleCursor()
        {
            if (!GetCursorPos(out var cursorPosition) ||
                !_colorSampler.TrySample(cursorPosition.X, cursorPosition.Y, out var color))
            {
                _pickerState.Fail();
                return false;
            }

            if (!_pickerState.Preview(color))
            {
                return false;
            }

            UpdateSamplePreview(color, cursorPosition);
            SampledColorChanged?.Invoke(color);
            return true;
        }

        private void UpdateSamplePreview(Color color, CursorPosition cursorPosition)
        {
            SampleColorSwatch.Background = new SolidColorBrush(color);
            SampleColorHex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            SamplePreview.Visibility = Visibility.Visible;

            var cursor = PointFromScreen(new Point(cursorPosition.X, cursorPosition.Y));
            const double cursorGap = 18;
            var left = cursor.X - SamplePreview.Width - cursorGap;
            var top = cursor.Y - SamplePreview.Height - cursorGap;

            if (left < 0)
            {
                left = cursor.X + cursorGap;
            }

            if (top < 0)
            {
                top = cursor.Y + cursorGap;
            }

            left = Math.Clamp(left, 0, Math.Max(0, OverlayCanvas.ActualWidth - SamplePreview.Width));
            top = Math.Clamp(top, 0, Math.Max(0, OverlayCanvas.ActualHeight - SamplePreview.Height));
            Canvas.SetLeft(SamplePreview, left);
            Canvas.SetTop(SamplePreview, top);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out CursorPosition position);

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorPosition
        {
            public int X;
            public int Y;
        }
    }
}
