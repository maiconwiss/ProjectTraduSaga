using System;
using System.Windows;
using System.Windows.Controls;
using TraduSaga.MVVM.ViewModels;
using TraduSaga.Utils;

namespace TraduSaga.MVVM.Views
{
    /// <summary>
    /// Interaction logic for LanguagesSettingsView.xaml
    /// </summary>
    public partial class LanguagesSettingsView : UserControl
    {
        private LanguagesSettingsViewModel _loadedViewModel;

        public LanguagesSettingsView()
        {
            InitializeComponent();
            Loaded += LanguagesSettingsView_Loaded;
        }

        private LanguagesSettingsViewModel ViewModel =>
            DataContext as LanguagesSettingsViewModel
            ?? throw new InvalidOperationException("The language settings view model is unavailable.");

        private void SaveDeeplApiKey_Click(object sender, RoutedEventArgs e)
        {
            SaveApiKey(DeeplApiKeyInput, ViewModel.SetDeeplApiKey);
        }

        private void SaveYandexApiKey_Click(object sender, RoutedEventArgs e)
        {
            SaveApiKey(YandexApiKeyInput, ViewModel.SetYandexApiKey);
        }

        private static void SaveApiKey(PasswordBox input, Action<string> save)
        {
            if (string.IsNullOrWhiteSpace(input.Password))
            {
                MessageBox.Show(
                    LocalizationManager.GetValue("Str.LangSettings.ApiKeyEntryRequired", true),
                    "TraduSaga",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            save(input.Password);
            input.Clear();
        }

        private void ToggleDeeplApiKeyReveal_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleDeeplApiKeyReveal();
        }

        private void ToggleYandexApiKeyReveal_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleYandexApiKeyReveal();
        }

        private void DeleteDeeplApiKey_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDeleteApiKey())
            {
                ViewModel.DeleteDeeplApiKey();
            }
        }

        private void DeleteYandexApiKey_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDeleteApiKey())
            {
                ViewModel.DeleteYandexApiKey();
            }
        }

        private void ForgetGoogleConsent_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ForgetGoogleConsent();
        }

        private void ForgetDeeplConsent_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ForgetDeeplConsent();
        }

        private void ForgetYandexConsent_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ForgetYandexConsent();
        }

        private static bool ConfirmDeleteApiKey()
        {
            return MessageBox.Show(
                LocalizationManager.GetValue("Str.LangSettings.DeleteApiKeyQuestion", true),
                LocalizationManager.GetValue("Str.LangSettings.DeleteApiKeyTitle", true),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private void LanguagesSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            _loadedViewModel?.HideApiKeys();
            _loadedViewModel = null;
            DeeplApiKeyInput.Clear();
            YandexApiKeyInput.Clear();
        }

        private void LanguagesSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            _loadedViewModel = DataContext as LanguagesSettingsViewModel;
        }
    }
}
