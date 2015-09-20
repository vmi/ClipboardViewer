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
                view.FontFamily = new FontFamily(settings.FontName);
            if (settings.FontSize > 0)
                view.FontSize = settings.FontSize;
            if (settings.WindowWidth > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }
            setTopmost(settings.Topmost, false);
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
            view.Inlines.Clear();
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
                        view.Inlines.Add(item);
                        continue;
                    }
                    var tbMatch = match.Groups["tb"];
                    var tbLen = tbMatch.Length;
                    if (tbLen > 0)
                    {
                        var tbs = new StringBuilder(tbLen * 4);
                        for (int i = 0; i < tbLen; i++)
                            tbs.Append("\ue001\ue003");
                        var item = new TextBlock(new Run(tbs.ToString()));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        item.LayoutTransform = new ScaleTransform(2, 1);
                        view.Inlines.Add(item);
                        continue;
                    }
                    var spMatch = match.Groups["sp"];
                    if (spMatch.Length > 0)
                    {
                        var item = new TextBlock(new Run(new string(' ', spMatch.Length)));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        item.LayoutTransform = new ScaleTransform(0.75, 1);
                        view.Inlines.Add(item);
                        continue;
                    }
                    var wsMatch = match.Groups["ws"];
                    if (wsMatch.Length > 0)
                    {
                        var item = new Run(new string('\ue005', wsMatch.Length));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        view.Inlines.Add(item);
                        continue;
                    }
                    var csMatch = match.Groups["cs"];
                    if (csMatch.Length > 0)
                    {
                        var item = new Run(string.Format("\\u{0,0:X4}", csMatch.Value[0]));
                        item.FontFamily = wsFontFamily;
                        item.Foreground = Brushes.Red;
                        view.Inlines.Add(item);
                        continue;
                    }
                    view.Inlines.Add(new Run(match.Groups["ch"].Value));
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
                view.Inlines.Add(item);
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

        public void Dispose()
        {
            clipboardHelper.Dispose();
        }

        private void TopMost_Initialized(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
            menuItem.IsChecked = Topmost;
        }
        private void setTopmost(bool isTopmost, bool isSave)
        {
            if (origTitle == null)
                origTitle = Title;
            Topmost = isTopmost;
            if (isTopmost)
                Title = origTitle + " (Top Most)";
            else
                Title = origTitle;
            if (isSave)
            {
                var settings = Properties.Settings.Default;
                settings.Topmost = isTopmost;
                settings.Save();
            }
        }

        private void Topmost_Checked(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            setTopmost(menuItem.IsChecked, true);
        }

        private void SelectFont_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FontDialog();
            string curname = view.FontFamily.Source;
            float cursize = (float)(view.FontSize * 72.0 / 96.0);
            dialog.Font = new System.Drawing.Font(curname, cursize);
            dialog.ShowEffects = false;
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            var f = dialog.Font;
            string newname = f.FontFamily.Name;
            double newsize = f.SizeInPoints * 96.0 / 72.0;
            view.FontFamily = new FontFamily(newname);
            view.FontSize = newsize;
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
            ClipboardViewer_SaveSizeAndLocation(sender, (EventArgs)e);
        }
    }
}
