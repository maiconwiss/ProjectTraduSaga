using System.Windows.Media;
using TraduSaga.Utils;

namespace TraduSaga.Security.Tests;

public class HexColorParserTests
{
    [Theory]
    [InlineData("#1E1E1E", 30, 30, 30)]
    [InlineData("1e1e1e", 30, 30, 30)]
    [InlineData("#00A7FF", 0, 167, 255)]
    [InlineData("000000", 0, 0, 0)]
    public void TryParseRgbAcceptsSixDigitRgbWithOptionalHash(string value, byte red, byte green, byte blue)
    {
        Assert.True(HexColorParser.TryParseRgb(value, out var color));
        Assert.Equal(Color.FromRgb(red, green, blue), color);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("#1234567")]
    [InlineData("#12GG56")]
    [InlineData("#12 456")]
    public void TryParseRgbRejectsMalformedValues(string value)
    {
        Assert.False(HexColorParser.TryParseRgb(value, out _));
    }

    [Fact]
    public void FormatRgbReturnsUppercaseHashPrefixedRgb()
    {
        Assert.Equal("#1E0AFF", HexColorParser.FormatRgb(Color.FromRgb(30, 10, 255)));
    }
}
