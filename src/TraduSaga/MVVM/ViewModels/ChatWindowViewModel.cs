using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraduSaga.Dialog;
using TraduSaga.HotKeys;
using TraduSaga.Infrastructure;
using TraduSaga.Infrastructure.Constants;
using TraduSaga.MVVM.Models;
using TraduSaga.Services;
using TraduSaga.Utils;
using RelayCommand = Microsoft.Toolkit.Mvvm.Input.RelayCommand;

namespace TraduSaga.MVVM.ViewModels
{
    public sealed class ChatWindowViewModel : BindableBase
    {
        public ChatWindowModel Model { get; set; }
        public bool ChatWindowIsVisible
        {
            get => _chatWindowIsVisible;
            set => SetProperty(ref _chatWindowIsVisible, value);
        }

        public ICommand ShowHideSettingsCommand => new RelayCommand(OnShowHideSettings);
        public ICommand ShowHideChatCommand => new RelayCommand(OnShowHideChat);
        public ICommand DismissUpdateNoticeCommand => new RelayCommand(DismissUpdateNotice);
        public ICommand OpenUpdatePageCommand => new RelayCommand(OpenUpdatePage);

        public bool UpdateNoticeVisible => _updateNoticeState.IsVisible;

        public string UpdateNoticeText => _updateNoticeState.GetText(
            LocalizationManager.GetValue("Str.Update.Available", true) ?? "Update available: {0} → {1}");

        private bool _chatWindowIsVisible = true;
        private readonly UpdateNoticeState _updateNoticeState = new UpdateNoticeState(Global.GetVersion());

        private readonly DialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly HotKeysServiceManager _hotKeysServiceManager;
        private bool _helpTextDisplayed;

        public ChatWindowViewModel(ChatWindowModel model, HotKeysServiceManager hotKeysManager, ChatUITextMediator chatTextMediator,
            DialogService dialogService, IServiceProvider serviceProvider, ILogger<ChatWindowViewModel> logger)
        {
            this.Model = model;
            this._logger = logger;
            this._dialogService = dialogService;
            this._serviceProvider = serviceProvider;
            this._hotKeysServiceManager = hotKeysManager;

            hotKeysManager.SelectAreaKeyPressed += HotKeysManagerOnSelectAreaKeyPressed;
            hotKeysManager.TranslationStateKeyPressed += HotKeysManagerOnTranslationStateKeyPressed;
            hotKeysManager.ChatVisibilityKeyPressed += HotKeysManagerOnChatVisibilityKeyPressed;
            hotKeysManager.SettingVisibilityKeyPressed += HotKeysManagerOnSettingVisibilityKeyPressed;
            hotKeysManager.ShowSelectionAreaKeyPressed += HotKeysManagerOnShowSelectionAreaKeyPressed;
            hotKeysManager.OnceTranslateKeyPressed += HotKeysManagerOnOnceTranslateKeyPressed;
            hotKeysManager.WindowStyleChangeKeyPressed += HotKeysManagerOnWindowStyleChangeKeyPressed;
            chatTextMediator.TextRaised += ChatTextMediatorOnTextRaised;
            chatTextMediator.ClearTextsRaised += ChatTextMediatorOnClearTextsRaised;
            Model.ChatFirstItemsRemoved += ModelOnChatFirstItemsRemoved;
            LocalizationManager.CultureChanged += LocalizationManagerOnCultureChanged;
        }

        private void HotKeysManagerOnSettingVisibilityKeyPressed(object sender, EventArgs e)
        {
            OnShowHideSettings();
        }

        private void ChatTextMediatorOnTextRaised(object sender, TranslatedEventArgs e)
        {
            Model.AddChatItem(e.Text, e.TextType);
        }

        private void ChatTextMediatorOnClearTextsRaised(object sender, EventArgs e)
        {
            Model.ClearAllChatItems();
        }

        private void ModelOnChatFirstItemsRemoved(object sender, ChatFirstItemsRemovedEventArgs e)
        {
            if (e.NumberRemoved >= 4)
            {
                _helpTextDisplayed = false;
            }
        }

        private void LocalizationManagerOnCultureChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(UpdateNoticeText));
            if (!_helpTextDisplayed)
            {
                return;
            }

            Model.RemoveFirstChatItems(4);
            SendHelpText();
        }


        private void HotKeysManagerOnChatVisibilityKeyPressed(object sender, EventArgs e)
        {
            OnShowHideChat();
        }

        private void HotKeysManagerOnTranslationStateKeyPressed(object sender, EventArgs e)
        {
            if (Model.TranslationIsRunning)
            {
                Model.EndTranslation();
            }
            else
            {
                Model.StartTranslation();
            }
        }

        private void HotKeysManagerOnSelectAreaKeyPressed(object sender, EventArgs e)
        {
            Model.EndTranslation();
            
            var result = _dialogService.ShowWindowDialog<SelectionAreaWindow>(out var window);
            if (result.HasValue && result.Value)
            {
                Model.CaptureConfiguration.CaptureArea = window.SelectedArea;
            }
        }

        private void HotKeysManagerOnShowSelectionAreaKeyPressed(object sender, EventArgs e)
        {
            if (!Model.CaptureConfiguration.CaptureArea.IsEmpty)
            {
                _dialogService.ShowWindowDialog<SelectionAreaWindow>(out _, Model.CaptureConfiguration.CaptureArea);
            }
        }

        private void HotKeysManagerOnOnceTranslateKeyPressed(object sender, EventArgs e)
        {
            var result = _dialogService.ShowWindowDialog<SelectionAreaWindow>(out var window);
            if (result.HasValue && result.Value)
            {
                Model.OnceTranslation(window.SelectedArea);
            }
        }

        private void HotKeysManagerOnWindowStyleChangeKeyPressed(object sender, EventArgs e)
        { 
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int GWL_EXSTYLE = -20;

            IntPtr hwnd = _dialogService.GetWindowHandle<ChatWindowViewModel>();
            int extendedStyle = Win32Interfaces.GetWindowLong(hwnd, GWL_EXSTYLE);
            Win32Interfaces.SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle ^ WS_EX_TRANSPARENT);
            
            bool isLocked = (extendedStyle | WS_EX_TRANSPARENT) != extendedStyle;
            Model.AddChatItem(LocalizationManager.GetValue(isLocked ? "Str.Chat.WindowLocked" : "Str.Chat.WindowUnlocked"), TextTypes.Info);
        }

        private void OnShowHideSettings()
        {
            if (!_dialogService.CloseWindow<SettingsViewModel>())
            {
                var scope = _serviceProvider.CreateScope();
                var viewModel = scope.ServiceProvider.GetService<SettingsViewModel>();
                _dialogService.ShowWindowAsync(viewModel, () =>
                {
                    scope.Dispose();
                    GC.Collect(2);
                });
            }
        }

        private void OnShowHideChat()
        {
            ChatWindowIsVisible = !ChatWindowIsVisible;
            if (ChatWindowIsVisible)
            {
                Model.StartTranslation();
            }
            else
            {
                Model.EndTranslation();
            }
        }

        public void ShowStartupHelp()
        {
            SendHelpText();
        }

        public void ShowUpdateNotice(Version availableVersion)
        {
            _updateNoticeState.Show(availableVersion);
            OnPropertyChanged(nameof(UpdateNoticeText));
            OnPropertyChanged(nameof(UpdateNoticeVisible));
        }

        private void OpenUpdatePage()
        {
            try
            {
                UpdateCheckService.OpenReleasesPage(url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open the Releases page");
            }
        }

        private void DismissUpdateNotice()
        {
            _updateNoticeState.Dismiss();
            OnPropertyChanged(nameof(UpdateNoticeText));
            OnPropertyChanged(nameof(UpdateNoticeVisible));
        }

        private void SendHelpText()
        {
            string GetHotKeyHelpText(string hotKeyName, string localizationKey)
            {
                return string.Format(LocalizationManager.GetValue(localizationKey), _hotKeysServiceManager.GetRegisteredKeyCaption(hotKeyName));
            }

            var configuration = _hotKeysServiceManager.Configuration;
            Model.AddChatItem(LocalizationManager.GetValue("Str.Hotkeys.GeneralHelp"), TextTypes.Info);
            Model.AddChatItem(GetHotKeyHelpText(nameof(configuration.ChatVisibilityKey), "Str.Hotkeys.ChatVisibilityHelp"), TextTypes.Info);
            Model.AddChatItem(GetHotKeyHelpText(nameof(configuration.SettingVisibilityKey), "Str.Hotkeys.SettingsShowHelp"), TextTypes.Info);
            Model.AddChatItem(GetHotKeyHelpText(nameof(configuration.SelectAreaKey), "Str.Hotkeys.SelectAreaHelp"), TextTypes.Info);
            Model.AddChatItem(GetHotKeyHelpText(nameof(configuration.TranslationStateKey), "Str.Hotkeys.OnTranslationHelp"), TextTypes.Info);
            _helpTextDisplayed = true;
        }
    }
}
