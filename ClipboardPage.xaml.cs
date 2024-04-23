using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;

namespace Memoryboard
{
    /// <summary>
    /// Interaction logic for ClipboardPage.xaml
    /// </summary>
    public partial class ClipboardPage : Page
    {
        private MainWindow _parentWindow;
        private string lastCopied;
        private bool _isInternalClipboardChange;

        public ClipboardPage()
        {
            InitializeComponent();
            lastCopied = string.Empty;
            Loaded += OnLoaded;
            _isInternalClipboardChange = false;
        }

        public void OnClipboardChanged()
        {
            if (_isInternalClipboardChange)
            {
                _isInternalClipboardChange = false;
            }
            else
            {
                string text = Clipboard.GetText();

                if (!string.IsNullOrEmpty(text) && text != lastCopied)
                {
                    lastCopied = text;

                    if (ClipboardList.Items.Count == 50)
                    {
                        Dispatcher.Invoke(() => ClipboardList.Items.RemoveAt(49));
                    }

                    Dispatcher.Invoke(() => ClipboardList.Items.Insert(0, text));
                    _parentWindow.SendCopyEvent(text);
                }
            }
        }

        public void BroadcastCopyReceived(string copiedText)
        {
            lastCopied = string.Empty;

            if (ClipboardList.Items.Count == 50)
            {
                Dispatcher.Invoke(() => ClipboardList.Items.RemoveAt(49));
            }

            Dispatcher.Invoke(() => ClipboardList.Items.Insert(0, copiedText));
        }

        public void BroadcastSelectReceived(int selectedIndex)
        {
            string selectedText = ClipboardList.Items.GetItemAt(selectedIndex).ToString();
            lastCopied = string.Empty;

            Dispatcher.Invoke(() =>
            {
                ClipboardList.Items.RemoveAt(selectedIndex);
                ClipboardList.Items.Insert(0, selectedText);

            });
        }

        public void BroadcastClearAllReceived()
        {
            Dispatcher.Invoke(() => ClipboardList.Items.Clear());
            lastCopied = string.Empty;
        }

        public void PopulateClipboard(List<string> clipboardItems)
        {
            Dispatcher.Invoke(() =>
            {
                ClipboardList.Items.Clear();

                foreach (string item in clipboardItems)
                {
                    ClipboardList.Items.Add(item);
                }
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this) as MainWindow;
        }

        private void ClipboardList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ClipboardList.SelectedItem != null)
            {
                string selectedText = ClipboardList.SelectedItem.ToString();
                int selectedIndex = ClipboardList.SelectedIndex;
                lastCopied = selectedText;

                Dispatcher.Invoke(() =>
                {
                    ClipboardList.Items.RemoveAt(selectedIndex);
                    ClipboardList.Items.Insert(0, selectedText);
                });

                _parentWindow.SendSelectEvent(selectedIndex);
                _isInternalClipboardChange = true;

                Clipboard.SetDataObject(selectedText);

                Hide();

                SendKeys.SendWait("^{v}");
            }
            else
            {
                Hide();
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => ClipboardList.Items.Clear());
            lastCopied = string.Empty;
            _parentWindow.SendClearAllEvent();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => ClipboardList.Items.Clear());
            lastCopied = string.Empty;

            _parentWindow.Logout();
        }

        private void Hide()
        {
            _parentWindow.Hide(); // Hide the parent window
        }
    }
}
