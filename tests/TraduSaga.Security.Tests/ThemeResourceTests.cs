using System.Windows;
using System.Windows.Media;

namespace TraduSaga.Security.Tests;

public class ThemeResourceTests
{
    [Theory]
    [InlineData("Dark", "#111318", "#1B2028", "#29313D", "#5DCAB5", "#111318", "#F3F5F7", "#AAB2C0", "#343B46", "#343B46")]
    [InlineData("Light", "#F4F6F8", "#FFFFFF", "#E9EDF2", "#2F8075", "#FFFFFF", "#1A2028", "#53606E", "#CDD4DE", "#E1E8EF")]
    public void ThemeDictionariesExposeSpecifiedTokensWithReadableText(
        string theme,
        string backgroundHex,
        string surfaceHex,
        string elevatedSurfaceHex,
        string accentHex,
        string accentTextHex,
        string primaryTextHex,
        string secondaryTextHex,
        string borderHex,
        string surfaceHoverHex)
    {
        RunOnSta(() =>
        {
            var resources = LoadDictionary($"Themes/{theme}.xaml");
            var background = ParseHexColor(backgroundHex);
            var surface = ParseHexColor(surfaceHex);
            var elevatedSurface = ParseHexColor(elevatedSurfaceHex);
            var accent = ParseHexColor(accentHex);
            var accentText = ParseHexColor(accentTextHex);
            var primaryText = ParseHexColor(primaryTextHex);
            var secondaryText = ParseHexColor(secondaryTextHex);

            Assert.Equal(background, GetBrush(resources, "AppBackgroundBrush").Color);
            Assert.Equal(surface, GetBrush(resources, "SurfaceBrush").Color);
            Assert.Equal(elevatedSurface, GetBrush(resources, "ElevatedSurfaceBrush").Color);
            Assert.Equal(accent, GetBrush(resources, "AccentBrush").Color);
            Assert.Equal(accentText, GetBrush(resources, "AccentTextBrush").Color);
            Assert.Equal(primaryText, GetBrush(resources, "PrimaryTextBrush").Color);
            Assert.Equal(secondaryText, GetBrush(resources, "SecondaryTextBrush").Color);
            Assert.Equal(ParseHexColor(borderHex), GetBrush(resources, "BorderBrush").Color);
            Assert.Equal(ParseHexColor(surfaceHoverHex), GetBrush(resources, "SurfaceHoverBrush").Color);

            Assert.True(GetContrastRatio(accent, accentText) >= 4.5);
            Assert.True(GetContrastRatio(primaryText, background) >= 4.5);
            Assert.True(GetContrastRatio(secondaryText, background) >= 4.5);
            Assert.True(GetContrastRatio(secondaryText, surface) >= 4.5);
            Assert.True(GetContrastRatio(secondaryText, elevatedSurface) >= 4.5);
        });
    }

    [Fact]
    public void MetricsDefineModernControlAndOverlayRadii()
    {
        RunOnSta(() =>
        {
            var resources = LoadDictionary("Themes/Metrics.xaml");

            Assert.Equal(new CornerRadius(6), resources["CompactControlCornerRadius"]);
            Assert.Equal(new CornerRadius(8), resources["ControlCornerRadius"]);
            Assert.Equal(new CornerRadius(12), resources["SurfaceCornerRadius"]);
            Assert.Equal(new CornerRadius(16), resources["OverlayCornerRadius"]);
        });
    }

    private static SolidColorBrush GetBrush(ResourceDictionary resources, string key)
    {
        return Assert.IsType<SolidColorBrush>(resources[key]);
    }

    private static Color ParseHexColor(string hex)
    {
        return Color.FromRgb(
            Convert.ToByte(hex.Substring(1, 2), 16),
            Convert.ToByte(hex.Substring(3, 2), 16),
            Convert.ToByte(hex.Substring(5, 2), 16));
    }

    private static ResourceDictionary LoadDictionary(string relativePath)
    {
        var uri = new Uri($"/TraduSaga;component/{relativePath}", UriKind.Relative);
        return Assert.IsType<ResourceDictionary>(Application.LoadComponent(uri));
    }

    private static double GetContrastRatio(Color first, Color second)
    {
        var firstLuminance = GetRelativeLuminance(first);
        var secondLuminance = GetRelativeLuminance(second);
        var lighter = Math.Max(firstLuminance, secondLuminance);
        var darker = Math.Min(firstLuminance, secondLuminance);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double GetRelativeLuminance(Color color)
    {
        static double Linearize(byte channel)
        {
            var value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linearize(color.R) + 0.7152 * Linearize(color.G) + 0.0722 * Linearize(color.B);
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
            throw new InvalidOperationException("The WPF resource check failed.", failure);
        }

    }
}
