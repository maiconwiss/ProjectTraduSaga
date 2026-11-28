using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SharpDX.XInput;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TraduSaga.Configuration;
using TraduSaga.Dialog;
using TraduSaga.HotKeys;
using TraduSaga.Infrastructure.Encryption;
using TraduSaga.Infrastructure.Constants;
using TraduSaga.Infrastructure.Language;
using TraduSaga.Infrastructure.MachineLearning;
using TraduSaga.MVVM.Models;
using TraduSaga.MVVM.ViewModels;
using TraduSaga.OCR;
using TraduSaga.OCR.Configuration;
using TraduSaga.Processing;
using TraduSaga.Processing.Configuration;
using TraduSaga.Processing.Interfaces;
using TraduSaga.Processing.TextProcessing;
using TraduSaga.Services;
using TraduSaga.Translation;
using TraduSaga.Translation.Configuration;
using TraduSaga.TTS;
using TraduSaga.Utils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TraduSaga
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly HashSet<string> _reportedErrors = new HashSet<string>();

        public App()
        {
            Log.Logger = CreateLogger();

            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            this._serviceProvider = services.BuildServiceProvider();
            this._logger = _serviceProvider.GetService<ILogger<App>>();

            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _logger.LogCritical(exception, "Unhandled app exception");
            ShowUnhandledException(exception);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.LogCritical(e.Exception, "Unhandled app exception");
            e.Handled = true;
            ShowUnhandledException(e.Exception);
        }

        private void ShowUnhandledException(Exception exception)
        {
            try
            {
                var message = exception?.Message ?? string.Empty;
                if (!_reportedErrors.Add(message))
                {
                    return;
                }

                var template = LocalizationManager.GetValue("Str.UnhandledError", true) ?? "Unexpected error: {0}";
                MessageBox.Show(string.Format(template, message), "TraduSaga", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to report unhandled exception");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            ThemeManager.StopFollowingSystemTheme();

            var configurationStorage = _serviceProvider.GetService<ConfigurationStorage>();
            if (configurationStorage.CanSaveConfiguration)
            {
                configurationStorage.SaveConfiguration();
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                OpenCvNativeLibraryResolver.Register();
                OpenCvNativeLibraryResolver.VerifyInstalledLibraries();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Native library integrity verification failed");
                MessageBox.Show(
                    LocalizationManager.GetValue("Str.App.NativeComponentsError", true),
                    "TraduSaga",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            ThemeManager.StartFollowingSystemTheme();

            var configurationStorage = _serviceProvider.GetService<ConfigurationStorage>();
            if (!configurationStorage.LoadConfiguration())
            {
                MessageBox.Show(
                    LocalizationManager.GetValue("Str.App.ConfigurationError", true),
                    "TraduSaga",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var captureConfiguration = _serviceProvider.GetService<ScreenCaptureConfiguration>();
            if (!WindowGeometryPolicy.IsCaptureAreaValid(captureConfiguration.CaptureArea, WindowGeometryPolicy.GetMonitorBounds()))
            {
                captureConfiguration.CaptureArea = System.Drawing.RectangleF.Empty;
            }

            if (!captureConfiguration.SpeakerArea.IsEmpty
                && !WindowGeometryPolicy.IsSpeakerAreaWithinCaptureArea(captureConfiguration.SpeakerArea, captureConfiguration.CaptureArea))
            {
                captureConfiguration.SpeakerArea = System.Drawing.RectangleF.Empty;
            }

            var systemConfiguration = _serviceProvider.GetService<SystemConfiguration>();
            LocalizationManager.ChangeAppCulture(new CultureInfo(systemConfiguration.ApplicationCulture));

            var chatViewModel = _serviceProvider.GetService<ChatWindowViewModel>();
            var dialogService = _serviceProvider.GetService<DialogService>();
            await dialogService.ShowWindowAsync(chatViewModel);
            chatViewModel.ShowStartupHelp();

            _serviceProvider.RegisterUIInputController();
            _ = CheckForUpdatesAsync(chatViewModel);
        }

        private async Task CheckForUpdatesAsync(ChatWindowViewModel chatViewModel)
        {
            try
            {
                var updateCheckService = _serviceProvider.GetService<UpdateCheckService>();
                var latestVersion = await updateCheckService.CheckForUpdateAsync(Global.GetVersion());
                if (latestVersion != null)
                {
                    chatViewModel.ShowUpdateNotice(latestVersion);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Update check failed");
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(builder => builder.AddSerilog(/*Log.Logger,*/ dispose: true));

            services.AddScoped<SettingsViewModel>();
            services.AddScoped<AppearanceSettingsViewModel>();
            services.AddScoped<HotkeysSettingsViewModel>();
            services.AddScoped<LanguagesSettingsViewModel>();
            services.AddScoped<OcrSettingsViewModel>();

            var chatWindowConfiguration = ChatWindowConfiguration.Default;
            services.AddSingleton<OcrGeneralConfiguration>(OcrGeneralConfiguration.Default);
            services.AddSingleton<TranslationConfiguration>(TranslationConfiguration.Default);
            services.AddSingleton<TtsConfiguration>(TtsConfiguration.Default);
            services.AddSingleton<ChatWindowConfiguration>(chatWindowConfiguration);
            services.AddSingleton<HotKeysConfiguration>(HotKeysConfiguration.Default);
            services.AddSingleton<SystemConfiguration>(SystemConfiguration.Default);
            services.AddSingleton<TextProcessingConfiguration>(chatWindowConfiguration.TextProcessing);

            var chatMediatorInstance = new ChatUITextMediator();
            services.AddSingleton<IChatTextMediator, ChatUITextMediator>(provider => chatMediatorInstance);
            services.AddSingleton<ChatUITextMediator>(chatMediatorInstance);
            services.AddSingleton<ChatWindowViewModel>();
            services.AddSingleton<ChatWindowModel>();
            services.AddSingleton<HotKeysServiceManager>();
            services.AddSingleton<ScreenCaptureConfiguration>();
            services.AddSingleton<ISpeakerAreaConfiguration>(provider => provider.GetRequiredService<ScreenCaptureConfiguration>());
            services.AddSingleton<TranslationWindowGeometry>();
            services.AddSingleton<DialogService>();
            services.AddSingleton<UpdateCheckService>();
            services.AddSingleton<LanguageService>();
            services.AddSingleton<TextDetectionProvider>();
            services.AddSingleton<TextValidityPredictor>();
            services.AddSingleton<IControllerService, GamepadService>();
            services.AddSingleton<IControllerInputProvider, ControllerInputProvider>();
            services.AddSingleton<ObservablePipe<Keystroke>>(new ObservablePipe<Keystroke>(Application.Current.Dispatcher));
            services.AddSingleton<ICapturerFactory, ScreenCapturerFactory>();
            services.AddTransient<IProcessingService, TranslationProcessingService>();
            services.AddTransient<OcrEnginesFactory>();
            services.AddTransient<TranslatorFactory>();
            services.AddTransient<TextResultCacheService>();
            services.AddTransient<IPredictor<InputTextPrediction, OutputTextPrediction>, MlPredictor<InputTextPrediction, OutputTextPrediction>>();
            services.AddSingleton<IDataProtectionService, DpapiDataProtectionService>();
            services.AddSingleton<ISecretStore, DpapiSecretStore>();
            services.AddSingleton<IProviderConsentPrompt, WpfProviderConsentPrompt>();
            services.AddSingleton<IProviderConsentService, ProviderConsentService>();
            services.AddTransient<LanguageDescriptorFactory>();
            services.AddTransient<TtsFactory>();


            services.AddConfigurationStorage();
        }

        private Logger CreateLogger()
        {
            var logDirectory = GetLogDirectoryPath();
            SecureFileStorage.EnsurePrivateDirectory(logDirectory);

            var configuration = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(logDirectory, "log.txt"), LogEventLevel.Warning, rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}", retainedFileCountLimit: 10);

#if DEBUG
            configuration = configuration.WriteTo.File(Path.Combine(logDirectory, "trace.txt"), LogEventLevel.Verbose, rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}");
#endif

            return configuration.CreateLogger();
        }

        internal static string GetLogDirectoryPath()
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData) || !Path.IsPathFullyQualified(localApplicationData))
            {
                throw new InvalidOperationException("The current user's local application data directory is unavailable.");
            }

            return Path.Combine(
                localApplicationData,
                "TraduSaga",
                "Logs");
        }

    }

    public static class NativeDialog
    {
        private const int MB_OK = 0x0;
        private const int MB_ICONERROR = 0x10;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        public static void ShowError(string message, string title = null)
        {
            // hWnd = IntPtr.Zero means no owner window
            MessageBoxW(
                IntPtr.Zero,
                message,
                title ?? LocalizationManager.GetValue("Str.App.ErrorTitle"),
                MB_OK | MB_ICONERROR);
        }
    }
}
