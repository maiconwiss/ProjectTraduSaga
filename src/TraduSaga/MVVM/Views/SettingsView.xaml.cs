using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TraduSaga.Infrastructure.Language;
using TraduSaga.MVVM.ViewModels;
using TraduSaga.MVVM.Common;
using TraduSaga.Services;
using TraduSaga.Utils;

namespace TraduSaga.MVVM.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        private SettingsViewModel ViewModel => DataContext as SettingsViewModel;

        public SettingsView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Activate();
        }

        private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.AdditionPanelOpened))
            {
                if (ViewModel.AdditionPanelOpened)
                {
                    this.BeginStoryboard(CreateStoryboardToOpenWindow(true), HandoffBehavior.SnapshotAndReplace, true);
                }
                else
                {
                    this.BeginStoryboard(CreateStoryboardToOpenWindow(false), HandoffBehavior.SnapshotAndReplace, true);
                }
            }
        }

        private Storyboard CreateStoryboardToOpenWindow(bool toOpen)
        {
            const int ADD_WINDOW_WIDTH = 205;
            var currentWidth = ActualWidth > 0 ? ActualWidth : Width;
            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                To = currentWidth + (toOpen ? ADD_WINDOW_WIDTH : -ADD_WINDOW_WIDTH),
                From = currentWidth,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Window.WidthProperty));
            Storyboard.SetTarget(widthAnimation, this);

            var storyboard = new Storyboard();
            storyboard.Children.Add(widthAnimation);

            return storyboard;
        }

        private void wSettings_SourceInitialized(object sender, EventArgs e)
        {
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void wSettings_Activated(object sender, EventArgs e)
        {
            //this.Topmost = true;
        }

        private void wSettings_Deactivated(object sender, EventArgs e)
        {
            //this.Topmost = true;
            //this.Activate();
        }

        private void wSettings_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void SnMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var sideNav = sender as MaterialDesignExtensions.Controls.SideNavigation;
            if (sideNav == null) return;

            // Check if there is at least one item
            if (sideNav.Items.Count > 0)
            {
                // Set the first item as selected
                sideNav.SelectedItem = (MaterialDesignExtensions.Model.INavigationItem)sideNav.Items[0];
            }
        }
    }
}
