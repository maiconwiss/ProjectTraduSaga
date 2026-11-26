using System.Windows.Media;
using TraduSaga.Configuration;
using TraduSaga.Services;

namespace TraduSaga.Security.Tests;

public class WindowsScreenColorSamplerTests
{
    [Fact]
    public void TryConvertColorRefConvertsWindowsBgrOrderToRgb()
    {
        Assert.True(WindowsScreenColorSampler.TryConvertColorRef(0x00030201, out var color));
        Assert.Equal(Color.FromRgb(1, 2, 3), color);
    }

    [Fact]
    public void TryConvertColorRefRejectsWindowsFailureSentinel()
    {
        Assert.False(WindowsScreenColorSampler.TryConvertColorRef(0xFFFFFFFF, out _));
    }

    [Fact]
    public void ChatWindowDoesNotReclaimFocusDuringScreenSampling()
    {
        ScreenColorSamplingSession.IsActive = true;
        try
        {
            Assert.False(ScreenColorSamplingSession.ShouldReactivateChatWindow);
        }
        finally
        {
            ScreenColorSamplingSession.IsActive = false;
        }

        Assert.True(ScreenColorSamplingSession.ShouldReactivateChatWindow);
    }

    [Fact]
    public void ChangingBackgroundColorDoesNotChangeBackgroundOpacity()
    {
        var configuration = new ChatWindowConfiguration { BackgroundOpacity = 0.42f };

        configuration.BackgroundColor = Color.FromRgb(12, 34, 56);

        Assert.Equal(0.42f, configuration.BackgroundOpacity);
    }
}
