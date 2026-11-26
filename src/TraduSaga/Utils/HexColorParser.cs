using System;
using System.Globalization;
using System.Windows.Media;

namespace TraduSaga.Utils
{
    internal static class HexColorParser
    {
        public static bool TryParseRgb(string value, out Color color)
        {
            color = default;
            if (value == null)
            {
                return false;
            }

            var hex = value.AsSpan();
            if (hex.Length == 7 && hex[0] == '#')
            {
                hex = hex[1..];
            }

            if (hex.Length != 6 ||
                !byte.TryParse(hex[..2], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var red) ||
                !byte.TryParse(hex.Slice(2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var green) ||
                !byte.TryParse(hex.Slice(4, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var blue))
            {
                return false;
            }

            color = Color.FromRgb(red, green, blue);
            return true;
        }

        public static string FormatRgb(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
