using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using TraduSaga.Utils;

namespace TraduSaga.Security.Tests;

public class LocalizationResourceTests
{
    [Fact]
    public void PortugueseBrazilianDictionaryCoversEnglishKeysAndPlaceholders()
    {
        RunOnSta(() =>
        {
            var english = LoadDictionary("Resources/Localization/lang.en-US.xaml");
            var portugueseBrazilian = LoadDictionary("Resources/Localization/lang.pt-BR.xaml");

            Assert.Equal(
                english.Keys.Cast<string>().OrderBy(key => key),
                portugueseBrazilian.Keys.Cast<string>().OrderBy(key => key));

            foreach (var key in english.Keys.Cast<string>())
            {
                var englishValue = Assert.IsType<string>(english[key]);
                var translatedValue = Assert.IsType<string>(portugueseBrazilian[key]);

                Assert.False(string.IsNullOrWhiteSpace(translatedValue), $"Missing translation for {key}.");
                Assert.Equal(GetPlaceholders(englishValue), GetPlaceholders(translatedValue));
                Assert.Equal(GetLineBreaks(englishValue), GetLineBreaks(translatedValue));
            }
        });
    }

    [Fact]
    public void PortugueseWindowsCultureSelectsPortugueseBrazilianLocalization()
    {
        Assert.Contains(
            LocalizationManager.AvailableLocalizations,
            culture => culture.Name == "pt-BR");
        Assert.Equal(
            "pt-BR",
            LocalizationManager.GetLocalizationForCulture(new CultureInfo("pt-BR")).Name);
        Assert.Equal(
            "pt-BR",
            LocalizationManager.GetLocalizationForCulture(new CultureInfo("pt-PT")).Name);
    }

    [Fact]
    public void SystemLocalizationUsesCurrentUserInterfaceCulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");

            Assert.Equal("pt-BR", LocalizationManager.GetSystemLocalization().Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void AboutIdentityUsesTradusagaNameAndOwnerInEveryLocalization()
    {
        RunOnSta(() =>
        {
            foreach (var culture in new[] { "en-US", "pt-BR", "ru-RU", "zh-CN" })
            {
                var dictionary = LoadDictionary($"Resources/Localization/lang.{culture}.xaml");
                Assert.Contains("TraduSaga", Assert.IsType<string>(dictionary["Str.Title.SettingWindow"]));
                Assert.Contains("Maicon Wissmann", Assert.IsType<string>(dictionary["Str.About.Authors"]));
            }
        });
    }

    private static ResourceDictionary LoadDictionary(string relativePath)
    {
        var uri = new Uri($"/TraduSaga;component/{relativePath}", UriKind.Relative);
        return Assert.IsType<ResourceDictionary>(Application.LoadComponent(uri));
    }

    private static string[] GetPlaceholders(string value)
    {
        return Regex.Matches(value, @"\{\d+\}")
            .Select(match => match.Value)
            .ToArray();
    }

    private static string[] GetLineBreaks(string value)
    {
        return Regex.Matches(value, @"\r\n|\r|\n")
            .Select(match => match.Value)
            .ToArray();
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure != null)
        {
            throw new InvalidOperationException("The localization resource check failed.", failure);
        }
    }
}
