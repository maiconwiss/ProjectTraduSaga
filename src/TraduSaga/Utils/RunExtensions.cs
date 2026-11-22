using System.Windows.Documents;

namespace TraduSaga.Utils
{
    public static class RunExtensions
    {
        public static Run Clone(this Run instance, string newText)
        {
            return new Run(newText)
            {
                Background = instance.Background,
                Foreground = instance.Foreground,
                FontWeight = instance.FontWeight,
                FontStyle = instance.FontStyle,
                FontSize = instance.FontSize,
            };
        }
    }
}
