using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using TraduSaga.MVVM.Common;
using TraduSaga.Translation;
using TraduSaga.Translation.Configuration;
using TraduSaga.Utils;

namespace TraduSaga.MVVM.Views
{
    public partial class ProviderConsentWindow : Window
    {
        private readonly string _privacyPolicyUrl;

        public ProviderConsentDecision Decision { get; private set; } = ProviderConsentDecision.Cancel;

        public ProviderConsentWindow(Translators provider)
        {
            InitializeComponent();
            var providerDetails = GetProviderDetails(provider);
            _privacyPolicyUrl = providerDetails.PrivacyPolicyUrl;
            Title = string.Format(LocalizationManager.GetValue("Str.Consent.Title", true), providerDetails.Name);
            HeadingText.Text = Title;
            ExplanationText.Text = string.Format(
                LocalizationManager.GetValue("Str.Consent.Explanation", true),
                providerDetails.Name);
            PrivacyPolicyLink.NavigateUri = new Uri(_privacyPolicyUrl);
            PrivacyPolicyLink.Inlines.Add(new Run(
                string.Format(LocalizationManager.GetValue("Str.Consent.PrivacyPolicy", true), providerDetails.Name)));

            if (Application.Current.MainWindow != null && Application.Current.MainWindow != this)
            {
                Owner = Application.Current.MainWindow;
            }
        }

        private void AllowButton_Click(object sender, RoutedEventArgs e)
        {
            Decision = ProviderConsentDecision.Allow;
            DialogResult = true;
        }

        private void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            Decision = ProviderConsentDecision.Deny;
            DialogResult = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Decision = ProviderConsentDecision.Cancel;
            DialogResult = false;
        }

        private void PrivacyPolicyLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            SafeExternalUrlPolicy.Open(_privacyPolicyUrl);
            e.Handled = true;
        }

        private static ProviderDetails GetProviderDetails(Translators provider)
        {
            return provider switch
            {
                Translators.Google => new ProviderDetails("Google", "https://policies.google.com/privacy"),
                Translators.Deepl => new ProviderDetails("DeepL", "https://www.deepl.com/privacy"),
                Translators.Yandex => new ProviderDetails("Yandex", "https://yandex.com/legal/confidential/"),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
        }

        private sealed class ProviderDetails
        {
            public string Name { get; }
            public string PrivacyPolicyUrl { get; }

            public ProviderDetails(string name, string privacyPolicyUrl)
            {
                Name = name;
                PrivacyPolicyUrl = privacyPolicyUrl;
            }
        }
    }
}
