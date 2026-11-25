using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using TraduSaga.Configuration;
using TraduSaga.Infrastructure;
using TraduSaga.MVVM.Models;
using TraduSaga.MVVM.ViewModels;
using TraduSaga.Utils;

namespace TraduSaga.MVVM.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ChatWindowView : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

        private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;
        private bool _restoringGeometry;

        public ChatWindowView()
        {
            InitializeComponent();
        }

        private void ModelOnChatFirstItemsRemoved(object sender, ChatFirstItemsRemovedEventArgs e)
        {
            RemoveFirstTextBlocks(e.NumberRemoved);
        }

        private void ModelOnChatItemAdded(object sender, ChatItemAddedEventArgs e)
        {
            AppendTextBlock(e.Text, e.TextType);
        }

        private void RemoveFirstTextBlocks(int numberBlocks)
        {
            var blocksCollection = rtbChat.Document.Blocks;
            var toDelete = Math.Min(numberBlocks, blocksCollection.Count);
            for (var i = 0; i < toDelete; i++)
            {
                blocksCollection.Remove(blocksCollection.FirstBlock);
            }
        }

        private void AppendTextBlock(string text, TextTypes textType)
        {
            if (string.IsNullOrEmpty(text))
                return;

            rtbChat.CaretPosition = rtbChat.CaretPosition.DocumentEnd;

            var paragraph = new Paragraph { LineHeight = fontTextBlockInstance.LineHeight, TextAlignment = fontTextBlockInstance.TextAlignment};
            var run = GetRunInstance(textType).Clone(text);
            paragraph.Inlines.Add(run);
            rtbChat.Document.Blocks.Add(paragraph);

            rtbChat.ScrollToEnd();
        }

        private void rtbChat_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.Topmost = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Topmost = true;
            this.Activate();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var viewModel = DataContext as ChatWindowViewModel;

            RestoreWindowGeometry(viewModel.Model.WindowGeometry);

            viewModel.Model.ChatItemAdded += ModelOnChatItemAdded;
            viewModel.Model.ChatFirstItemsRemoved += ModelOnChatFirstItemsRemoved;
            viewModel.Model.Configuration.ExcludeFromCaptueChanged += Configuration_ExcludeFromCaptueChanged;

            ExcludeFromCapture(viewModel.Model.Configuration.ExcludeFromCaptue);
        }

        private void RestoreWindowGeometry(TranslationWindowGeometry geometry)
        {
            var bounds = WindowGeometryPolicy.Restore(
                geometry.Left,
                geometry.Top,
                geometry.Width,
                geometry.Height,
                WindowGeometryPolicy.GetMonitorWorkAreas(),
                Width,
                Height);

            _restoringGeometry = true;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
            _restoringGeometry = false;

            geometry.Left = Left;
            geometry.Top = Top;
            geometry.Width = Width;
            geometry.Height = Height;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SaveWindowGeometry();
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            SaveWindowGeometry();
        }

        private void SaveWindowGeometry(TranslationWindowGeometry geometry = null)
        {
            if (_restoringGeometry || WindowState != WindowState.Normal || !IsLoaded)
            {
                return;
            }

            geometry ??= (DataContext as ChatWindowViewModel)?.Model.WindowGeometry;
            if (geometry == null || double.IsNaN(Left) || double.IsInfinity(Left)
                || double.IsNaN(Top) || double.IsInfinity(Top)
                || double.IsNaN(ActualWidth) || double.IsInfinity(ActualWidth) || ActualWidth <= 0
                || double.IsNaN(ActualHeight) || double.IsInfinity(ActualHeight) || ActualHeight <= 0)
            {
                return;
            }

            geometry.Left = Left;
            geometry.Top = Top;
            geometry.Width = ActualWidth;
            geometry.Height = ActualHeight;
        }

        private void Configuration_ExcludeFromCaptueChanged(object sender, EventArgs e)
        {
            if (sender is ChatWindowConfiguration config)
            {
                bool exclude = config.ExcludeFromCaptue;
                ExcludeFromCapture(exclude);
            }
        }

        // Delay setting display affinity until window handle is created
        // Exclude ChatWindow to be captured in screenshot
        // Requires build 10.0.19041
        private void ExcludeFromCapture(bool exclude)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            if (exclude)
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
            else
                SetWindowDisplayAffinity(hwnd, 0); // allow capture
        }

        private Run GetRunInstance(TextTypes textType)
        {
            return textType switch
            {
                TextTypes.Info => fontRunInfoInstance,
                TextTypes.Error => fontRunErrorInstance,
                _ => fontRunInstance,
            };
        }
    }
}
