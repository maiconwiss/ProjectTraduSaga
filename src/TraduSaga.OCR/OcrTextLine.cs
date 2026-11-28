using System.Collections.Generic;
using System.Drawing;

namespace TraduSaga.OCR
{
    public sealed class OcrTextLine
    {
        public string Text { get; }
        public IReadOnlyList<OcrTextWord> Words { get; }

        public OcrTextLine(string text, IReadOnlyList<OcrTextWord> words)
        {
            Text = text;
            Words = words;
        }
    }

    public sealed class OcrTextWord
    {
        public string Text { get; }
        public RectangleF Bounds { get; }

        public OcrTextWord(string text, RectangleF bounds)
        {
            Text = text;
            Bounds = bounds;
        }
    }
}
