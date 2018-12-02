using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;

namespace Chat.Server
{
    public static class TextExtension
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            Brush brush = new SolidColorBrush(color);

            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
            catch (FormatException) { }
        }
    }
}
