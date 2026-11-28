using System.Drawing;
using System.Windows;
using TraduSaga.Configuration;

namespace TraduSaga.Security.Tests;

public class WindowGeometryPolicyTests
{
    [Fact]
    public void SpeakerAreaMustBePositiveAndContainedByTheMainCaptureArea()
    {
        var captureArea = new RectangleF(100, 200, 640, 360);

        Assert.True(WindowGeometryPolicy.IsSpeakerAreaWithinCaptureArea(new RectangleF(120, 220, 100, 40), captureArea));
        Assert.True(WindowGeometryPolicy.IsSpeakerAreaWithinCaptureArea(captureArea, captureArea));
        Assert.False(WindowGeometryPolicy.IsSpeakerAreaWithinCaptureArea(new RectangleF(90, 220, 100, 40), captureArea));
        Assert.False(WindowGeometryPolicy.IsSpeakerAreaWithinCaptureArea(new RectangleF(120, 220, 0, 40), captureArea));
        Assert.False(WindowGeometryPolicy.IsSpeakerAreaWithinCaptureArea(RectangleF.Empty, captureArea));
    }

    [Fact]
    public void Restore_KeepsSavedBoundsWhenTheyAreInsideAWorkArea()
    {
        var bounds = WindowGeometryPolicy.Restore(
            100, 120, 550, 250,
            [new Rect(0, 0, 1920, 1080)],
            550, 250);

        Assert.Equal(new Rect(100, 120, 550, 250), bounds);
    }

    [Fact]
    public void Restore_RepositionsWindowSoItRemainsVisible()
    {
        var bounds = WindowGeometryPolicy.Restore(
            3000, 120, 550, 250,
            [new Rect(0, 0, 1920, 1080)],
            550, 250);

        Assert.Equal(new Rect(1840, 120, 550, 250), bounds);
    }

    [Fact]
    public void Restore_PreservesSavedSizeEvenWhenItExceedsTheWorkArea()
    {
        var bounds = WindowGeometryPolicy.Restore(
            3000, 120, 2500, 2000,
            [new Rect(0, 0, 1920, 1080)],
            550, 250);

        Assert.Equal(new Rect(1840, 120, 2500, 2000), bounds);
    }

    [Fact]
    public void Restore_UsesDefaultsForInvalidDimensionsAndCoordinates()
    {
        var bounds = WindowGeometryPolicy.Restore(
            double.NaN, double.PositiveInfinity, -1, 0,
            [new Rect(0, 0, 1920, 1080)],
            550, 250);

        Assert.Equal(new Rect(0, 0, 550, 250), bounds);
    }

    [Fact]
    public void IsCaptureAreaValid_RequiresAreaToFitInsideOneConnectedMonitor()
    {
        RectangleF[] monitors = [new(0, 0, 1920, 1080), new(1920, 0, 1920, 1080)];

        Assert.True(WindowGeometryPolicy.IsCaptureAreaValid(new RectangleF(100, 100, 400, 300), monitors));
        Assert.False(WindowGeometryPolicy.IsCaptureAreaValid(new RectangleF(1800, 100, 300, 300), monitors));
        Assert.False(WindowGeometryPolicy.IsCaptureAreaValid(new RectangleF(-10, 100, 300, 300), monitors));
        Assert.False(WindowGeometryPolicy.IsCaptureAreaValid(new RectangleF(100, 100, 0, 300), monitors));
    }
}
