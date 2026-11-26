using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System;
using System.ComponentModel;
using MaterialDesignThemes.Wpf;
using TraduSaga.MVVM.Views;
using TraduSaga.Services;
using TraduSaga.Utils;

namespace TraduSaga.Controls
{
    /// <summary>
    /// Interaction logic for ColorSelectionControl.xaml
    /// </summary>
    public partial class ColorSelectionControl : UserControl
    {
        public static readonly DependencyProperty ShowAdvancedColorOptionsProperty =
            DependencyProperty.Register(nameof(ShowAdvancedColorOptions), typeof(bool), typeof(ColorSelectionControl));

        public static DependencyProperty ColorPickedCommandProperty
            = DependencyProperty.Register("ColorPickedCommand", typeof(ICommand), typeof(ColorSelectionControl));

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                "SelectedColor", typeof(Color), typeof(ColorSelectionControl),
                new FrameworkPropertyMetadata(
                    defaultValue:default(Color), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedColorChangedCallback));

        private static void SelectedColorChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((Color)e.NewValue == (Color)e.OldValue)
            {
                return;
            }

            var targetControl = (ColorSelectionControl)d;
            targetControl.cpWindowColor.Color = (Color) e.NewValue;
        }

        private static readonly DependencyPropertyDescriptor ColorPropertyDescriptor =
            DependencyPropertyDescriptor.FromName(nameof(ColorPicker.Color), typeof(ColorPicker), typeof(ColorPicker));

        private bool _isUpdatingColorFromHex;
        private ScreenColorPickerWindow _activePicker;
        private ScreenColorSelectionDraft _sampleDraft;

        public bool ShowAdvancedColorOptions
        {
            get => (bool)GetValue(ShowAdvancedColorOptionsProperty);
            set => SetValue(ShowAdvancedColorOptionsProperty, value);
        }

        public ICommand ColorPickedCommand
        {
            get { return (ICommand)GetValue(ColorPickedCommandProperty); }
            set { SetValue(ColorPickedCommandProperty, value); }
        }

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public string Caption
        {
            get => (string)lCaption.Content;
            set => lCaption.Content = value;
        }

        public ColorSelectionControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ColorPropertyDescriptor?.AddValueChanged(cpWindowColor, OnPaletteColorChanged);
            OnPaletteColorChanged(cpWindowColor, EventArgs.Empty);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ColorPropertyDescriptor?.RemoveValueChanged(cpWindowColor, OnPaletteColorChanged);
        }

        private void OnPaletteColorChanged(object sender, EventArgs e)
        {
            if (_isUpdatingColorFromHex)
            {
                return;
            }

            var color = cpWindowColor.Color;
            _isUpdatingColorFromHex = true;
            HexColorInput.Text = HexColorParser.FormatRgb(color);
            SetValue(SelectedColorProperty, color);
            _isUpdatingColorFromHex = false;
            SetHexInputValidity(true);
        }

        private void HexColorInput_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingColorFromHex)
            {
                return;
            }

            if (!HexColorParser.TryParseRgb(HexColorInput.Text, out var color))
            {
                SetHexInputValidity(false);
                return;
            }

            _isUpdatingColorFromHex = true;
            cpWindowColor.Color = color;
            SetValue(SelectedColorProperty, color);
            _isUpdatingColorFromHex = false;
            SetHexInputValidity(true);
        }

        private void SetHexInputValidity(bool isValid)
        {
            HexColorError.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
            BtnApply.IsEnabled = isValid;
        }

        private void EyedropperButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_activePicker != null)
            {
                return;
            }

            var picker = new ScreenColorPickerWindow();
            var owner = Window.GetWindow(this);
            if (owner != null)
            {
                picker.Owner = owner;
            }

            _sampleDraft = new ScreenColorSelectionDraft(cpWindowColor.Color, HexColorInput.Text, BtnApply.IsEnabled);
            _activePicker = picker;
            picker.SampledColorChanged += OnSampledColorChanged;
            picker.Closed += OnScreenColorPickerClosed;
            ScreenSampleError.Visibility = Visibility.Collapsed;
            ScreenColorSamplingSession.IsActive = true;
            try
            {
                picker.Show();
            }
            catch
            {
                ScreenColorSamplingSession.IsActive = false;
                _activePicker = null;
                throw;
            }
        }

        private void OnSampledColorChanged(Color color)
        {
            _sampleDraft.Preview(color);
            SetDraftColor(color);
            ScreenSampleError.Visibility = Visibility.Collapsed;
        }

        private void OnScreenColorPickerClosed(object sender, EventArgs e)
        {
            var picker = (ScreenColorPickerWindow)sender;
            _activePicker = null;

            if (picker.SelectedColor.HasValue)
            {
                _sampleDraft.Preview(picker.SelectedColor.Value);
                SetDraftColor(_sampleDraft.Color);
                ScreenSampleError.Visibility = Visibility.Collapsed;
            }
            else
            {
                _sampleDraft.Cancel();
                SetDraftColor(_sampleDraft.Color);
                HexColorInput.Text = _sampleDraft.HexText;
                SetHexInputValidity(_sampleDraft.IsValid);
                if (picker.SamplingFailed)
                {
                    ScreenSampleError.Visibility = Visibility.Visible;
                }
            }

            try
            {
                Window.GetWindow(this)?.Activate();
            }
            finally
            {
                ScreenColorSamplingSession.IsActive = false;
            }
        }

        private void SetDraftColor(Color color)
        {
            _isUpdatingColorFromHex = true;
            cpWindowColor.Color = color;
            HexColorInput.Text = HexColorParser.FormatRgb(color);
            SetValue(SelectedColorProperty, color);
            _isUpdatingColorFromHex = false;
            SetHexInputValidity(true);
        }
        
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            SetValue(SelectedColorProperty, cpWindowColor.Color);
            ColorPickedCommand.Execute(true);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ColorPickedCommand.Execute(false);
        }
    }
}
