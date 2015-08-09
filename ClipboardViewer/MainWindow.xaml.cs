using System;
using System.Collections.Generic;
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
        private static Regex TOKEN_RE = new Regex(@"\G(?:(?<nl>(?:\r\n?|\n)+)|(?<tab>\t+)|(?<sp> +)|(?<wsp>\u3000+)|(?<ctrl[\p{Cc}\p{Zs}-[\r\n\t \u3000]]+)|(?<ch>[^\p{Cc}\p{Zs}]+))", RegexOptions.Singleline);
        private static string NL = Environment.NewLine;

        private string origTitle = null;
        private ClipboardHelper clipboardHelper = new ClipboardHelper();
        private FontFamily fontFamily = new FontFamily("Meiryo");
        private double fontSize = 12;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawClipboard()
        {
            richTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph();
            paragraph.FontFamily = fontFamily;
            paragraph.FontSize = fontSize;
            var textBlock = new TextBlock();
            paragraph.Inlines.Add(textBlock);
            if (Clipboard.ContainsText())
            {
                var cbText = Clipboard.GetText();
                for (var match = TOKEN_RE.Match(cbText); match.Success; match = match.NextMatch())
                {
                    var nl = match.Groups["nl"];
                    if (nl.Length > 0)
                    {
                        var len = nl.Value.Replace("\r\n", "\n").Length;
                        var text = new StringBuilder((1 + NL.Length) * len);
                        for (var i = 0; i < len; i++)
                            text.Append('⏎').Append(NL);
                        var elem = new Run(text.ToString());
                        elem.Foreground = Brushes.Red;
                        textBlock.Inlines.Add(elem);
                        continue;
                    }
                    var tab = match.Groups["tab"];
                    if (tab.Length > 0)
                    {
                        var text = new string('↦', tab.Length);
                        var elem = new TextBlock(new Run(text));
                        elem.Foreground = Brushes.Red;
                        elem.LayoutTransform = new ScaleTransform(2, 1);
                        textBlock.Inlines.Add(elem);
                        continue;
                    }
                    var sp = match.Groups["sp"];
                    if (sp.Length > 0)
                    {
                        var text = new string('␣', sp.Length);
                        var elem = new TextBlock(new Run(text));
                        elem.Foreground = Brushes.Red;
                        elem.Padding = new Thickness(1, 0, 1, 0);
                        elem.LayoutTransform = new ScaleTransform(0.75, 1);
                        textBlock.Inlines.Add(elem);
                        continue;
                    }
                    var wsp = match.Groups["wsp"];
                    if (wsp.Length > 0)
                    {
                        var text = new string('□', wsp.Length);
                        var elem = new Run(text);
                        elem.Foreground = Brushes.Red;
                        textBlock.Inlines.Add(elem);
                        continue;
                    }
                    var ctrl = match.Groups["ctrl"];
                    if (ctrl.Length > 0)
                    {
                        var text = new StringBuilder(ctrl.Length * 10);
                        char pc = '\u0000';
                        foreach (char c in ctrl.Value) {
                            if (char.IsHighSurrogate(c))
                            {
                                pc = c;
                                continue;
                            }
                            else if (char.IsLowSurrogate(c))
                            {
                                int cp = char.ConvertToUtf32(pc, c);
                                text.Append(string.Format("\\u{0:X6}", cp));
                            }
                            else if (c <= 0x1f)
                            {
                                text.Append('^').Append('@' + c);
                            }
                            else if (c == 0x80)
                            {
                                text.Append("^?");
                            }
                            else
                            {
                                text.Append(string.Format("\\u{0:X4}", c));
                            }
                        }
                        var elem = new Run(text.ToString());
                        elem.Foreground = Brushes.Red;
                        textBlock.Inlines.Add(elem);
                        continue;
                    }
                    var ch = match.Groups["ch"];
                    textBlock.Inlines.Add(new Run(match.Groups["ch"].Value));
                }
            }
            else
            {
                var notText = new Run("(not text)");
                notText.Foreground = Brushes.Firebrick;
                notText.FontStyle = FontStyles.Italic;
                textBlock.Inlines.Add(notText);
            }
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
