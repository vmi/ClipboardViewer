using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ClipboardViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public sealed partial class MainWindow : Window, IDisposable
    {
        private static Regex TOKEN_RE = new Regex(@"\G(?:(?<nl>(?:\r\n?|\n)+)|(?<tb>\t+)|(?<sp> +)|(?<ws>\u3000+)|(?<cs>[\p{Cc}\p{Zs}])|(?<ch>[^\p{Cc}\p{Zs}]+))", RegexOptions.Singleline);

        private string origTitle = null;
        private ClipboardHelper clipboardHelper = new ClipboardHelper();
        private FontFamily fontFamily = SystemFonts.MessageFontFamily;
        private FontFamily wsFontFamily = new FontFamily(new Uri("pack://application:,,,/Resources/"), "./#Whitespaces");
        private double fontSize = 12;

        public MainWindow()
        {
            InitializeComponent();
        }

        private Paragraph NewParagraph()
        {
            var paragraph = new Paragraph();
            paragraph.FontFamily = fontFamily;
            paragraph.FontSize = fontSize;
            return paragraph;
        }

        private enum CBStatus
        {
            Text,
            EmptyText,
            NotText,
            Exception
        }

        private void DrawClipboard()
        {
            richTextBox.Document.Blocks.Clear();
            var paragraph = NewParagraph();
            CBStatus cbStatus;
            string cbText;
            if (Clipboard.ContainsText())
            {
                try
                {
                    cbText = Clipboard.GetText();
                    cbStatus = cbText.Length > 0 ? CBStatus.Text : CBStatus.EmptyText;
                }
                catch (Exception e)
                {
                    cbText = e.Message;
                    cbStatus = CBStatus.Exception;
                }
            }
            else
            {
                cbText = null;
                cbStatus = CBStatus.NotText;
            }
            if (cbStatus == CBStatus.Text)
            {
                for (Match match = TOKEN_RE.Match(cbText); match.Success; match = match.NextMatch())
                {
                    var nlMatch = match.Groups["nl"];
                    if (nlMatch.Length > 0)
                    {
                        var len = nlMatch.Value.Replace("\r\n", "\n").Length;
                        var text = new StringBuilder(len * 2);
                        for (int i = 0; i < len; i++)
                            text.Append("\ue004\n");
                        var item = new Run(text.ToString());
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        paragraph.Inlines.Add(item);
                        continue;
                    }
                    var tbMatch = match.Groups["tb"];
                    var tbLen = tbMatch.Length;
                    if (tbLen > 0)
                    {
                        var tbs = new StringBuilder(tbLen * 4);
                        for (int i = 0; i < tbLen; i++)
                            tbs.Append("\ue001\ue002\ue002\ue003");
                        var item = new TextBlock(new Run(tbs.ToString()));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        item.LayoutTransform = new ScaleTransform(2, 1);
                        paragraph.Inlines.Add(item);
                        continue;
                    }
                    var spMatch = match.Groups["sp"];
                    if (spMatch.Length > 0)
                    {
                        var item = new TextBlock(new Run(new string(' ', spMatch.Length)));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        item.LayoutTransform = new ScaleTransform(0.75, 1);
                        paragraph.Inlines.Add(item);
                        continue;
                    }
                    var wsMatch = match.Groups["ws"];
                    if (wsMatch.Length > 0)
                    {
                        var item = new Run(new string('\u3000', wsMatch.Length));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        paragraph.Inlines.Add(item);
                        continue;
                    }
                    var csMatch = match.Groups["cs"];
                    if (csMatch.Length > 0)
                    {
                        var item = new Run(string.Format("\\u{0,0:X4}", csMatch.Value[0]));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        paragraph.Inlines.Add(item);
                        continue;
                    }
                    paragraph.Inlines.Add(new Run(match.Groups["ch"].Value));
                }
            }
            else
            {
                Inline item;
                switch (cbStatus)
                {
                    case CBStatus.EmptyText:
                        item = new Run("(empty)");
                        break;
                    case CBStatus.NotText:
                        item = new Run("(not text)");
                        break;
                    case CBStatus.Exception:
                        item = new Run("Failed to get a text from clipboard.\n[Exception]\n" + cbText);
                        break;
                    default:
                        // do not reached here.
                        item = null;
                        break;
                }
                item.Foreground = Brushes.Firebrick;
                item.FontStyle = FontStyles.Italic;
                paragraph.Inlines.Add(item);
            }
            richTextBox.Document.Blocks.Add(paragraph);
        }

        private void ClipboardViewer_ContentRendered(object sender, EventArgs e)
        {
            if (!clipboardHelper.IsRegistered())
                clipboardHelper.RegisterHandler(this, DrawClipboard);

        }

        private void ClipboardViewer_Closed(object sender, EventArgs e)
        {
            if (clipboardHelper.IsRegistered())
                clipboardHelper.DeregisterHandler();
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            Topmost = menuItem.IsChecked;
            if (origTitle == null)
                origTitle = Title;
            if (Topmost)
                Title = origTitle + " (Top Most)";
            else
                Title = origTitle;
        }

        public void Dispose()
        {
            clipboardHelper.Dispose();
        }
    }
}
