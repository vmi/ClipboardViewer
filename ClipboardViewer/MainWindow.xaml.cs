using System;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
        private static Regex TOKEN_RE = new Regex(@"\G(?:(?<sp>\r\n|[\p{Cc}\p{Zs}])|(?<ch>[^\p{Cc}\p{Zs}]+))", RegexOptions.Singleline);

        private string origTitle = null;
        private ClipboardHelper clipboardHelper = new ClipboardHelper();
        private FontFamily fontFamily = new FontFamily("Meiryo");
        private double fontSize = 12;

        public MainWindow()
        {
            InitializeComponent();
        }

        private Paragraph NextParagraph(Paragraph prevParagraph)
        {
            if (prevParagraph != null && prevParagraph.Inlines.Count > 0)
                richTextBox.Document.Blocks.Add(prevParagraph);
            var paragraph = new Paragraph();
            paragraph.FontFamily = fontFamily;
            paragraph.FontSize = fontSize;
            return paragraph;
        }

        private void DrawClipboard()
        {
            richTextBox.Document.Blocks.Clear();
            var paragraph = NextParagraph(null);
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                foreach (Match match in TOKEN_RE.Matches(text))
                {
                    var sp = match.Groups["sp"];
                    if (sp.Length > 0)
                    {
                        switch (sp.Value)
                        {
                            case "\r\n":
                            case "\r":
                            case "\n":
                                var nl = new Run("⏎");
                                nl.Foreground = Brushes.Red;
                                paragraph.Inlines.Add(nl);
                                paragraph = NextParagraph(paragraph);
                                break;

                            case "\t":
                                var tab = new TextBlock(new Run("↦"));
                                tab.Foreground = Brushes.Red;
                                tab.LayoutTransform = new ScaleTransform(2, 1);
                                paragraph.Inlines.Add(tab);
                                break;

                            case " ":
                                var spc = new TextBlock(new Run("␣"));
                                spc.Foreground = Brushes.Red;
                                spc.Padding = new Thickness(1, 0, 1, 0);
                                spc.LayoutTransform = new ScaleTransform(0.75, 1);
                                paragraph.Inlines.Add(spc);
                                break;

                            case "\u3000":
                                var wspc = new Run("□");
                                wspc.Foreground = Brushes.Red;
                                paragraph.Inlines.Add(wspc);
                                break;

                            default:
                                var esc = new Run(string.Format(@"\u{0:X4}", sp.Value[0]));
                                esc.Foreground = Brushes.Red;
                                paragraph.Inlines.Add(esc);
                                break;
                        }
                    }
                    else
                    {
                        paragraph.Inlines.Add(new Run(match.Groups["ch"].Value));
                    }
                }
            }
            else
            {
                var notText = new Run("(not text)");
                notText.Foreground = Brushes.Firebrick;
                notText.FontStyle = FontStyles.Italic;
                paragraph.Inlines.Add(notText);
            }
            if (paragraph.Inlines.Count > 0)
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
    }
}
