using System;
using System.Threading.Tasks;
using System.Windows;
using TraduSaga.MVVM.Views;
using TraduSaga.Translation;
using TraduSaga.Translation.Configuration;

namespace TraduSaga.Services
{
    public sealed class WpfProviderConsentPrompt : IProviderConsentPrompt
    {
        public Task<ProviderConsentDecision> PromptAsync(Translators provider)
        {
            var application = Application.Current
                ?? throw new InvalidOperationException("The WPF application is unavailable.");

            return application.Dispatcher.InvokeAsync(() =>
            {
                var window = new ProviderConsentWindow(provider);
                window.ShowDialog();
                return window.Decision;
            }).Task;
        }
    }
}
