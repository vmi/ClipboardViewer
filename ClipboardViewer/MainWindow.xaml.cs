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
        private FontFamily wsFontFamily = new FontFamily(new Uri("pack://application:,,,/Resources/"), "./#Whitespaces");

        public MainWindow()
        {
            InitializeComponent();
            var settings = Properties.Settings.Default;
            if (settings.FontName.Length > 0)
                richTextBox.FontFamily = new FontFamily(settings.FontName);
            if (settings.FontSize > 0)
                richTextBox.FontSize = settings.FontSize;
            if (settings.WindowWidth > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }
            
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
            var paragraph = new Paragraph();
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
                        var item = new Run(new string('\ue005', wsMatch.Length));
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

        private void SelectFont_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FontDialog();
            string curname = richTextBox.FontFamily.Source;
            float cursize = (float)(richTextBox.FontSize * 72.0 / 96.0);
            dialog.Font = new System.Drawing.Font(curname, cursize);
            dialog.ShowEffects = false;
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            var f = dialog.Font;
            string newname = f.FontFamily.Name;
            double newsize = f.SizeInPoints * 96.0 / 72.0;
            richTextBox.FontFamily = new FontFamily(newname);
            richTextBox.FontSize = newsize;
            var settings = Properties.Settings.Default;
            settings.FontName = newname;
            settings.FontSize = newsize;
            settings.Save();
        }

        private void ClipboardViewer_SaveSizeAndLocation(object sender, EventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.Save();
        }

        private void ClipboardViewer_SaveSizeAndLocation(object sender, SizeChangedEventArgs e)
        {
            ClipboardViewer_SaveSizeAndLocation(sender, (EventArgs) e);
        }
    }
}
