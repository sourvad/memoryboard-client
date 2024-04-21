using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;

namespace Memoryboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int MOD_WIN = 0x0008;
        private const int VK_V = 0x56;
        private const int WM_HOTKEY = 0x0312;

        private IntPtr windowHandle;
        private string lastCopied;
        private string lastPasted;
        private int lastIndex;

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AddClipboardFormatListener(IntPtr hwnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RemoveClipboardFormatListener(IntPtr hwnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Deactivated += OnDeactivated;
            Closed += OnClosed;
            lastCopied = string.Empty;
            lastPasted = string.Empty;
            lastIndex = -1;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(windowHandle)?.AddHook(WndProc);

            var hWnd = windowHandle;
            RegisterHotKey(hWnd, 0, MOD_WIN, VK_V); // ID for the hotkey is 0
            ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);

            // Start listening to clipboard changes
            AddClipboardFormatListener(windowHandle);

            Hide();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Stop listening to clipboard changes
            RemoveClipboardFormatListener(windowHandle);
        }

        private void OnDeactivated(object? sender, EventArgs e)
        {
            // Hide your window
            Hide();
        }
        private void OnClosed(object? sender, EventArgs e)
        {
            // Unregister the hotkey
            IntPtr handle = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(handle, 0);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                OnClipboardChanged();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == WM_HOTKEY)
            {
                // Show your window
                Show();
                Activate();
                handled = true;
            }
        }

        private void OnClipboardChanged()
        {
            // This method is called when the clipboard content changes
            // Example: Retrieving text from the clipboard
            string text = Clipboard.GetText();

            if (!string.IsNullOrEmpty(text) && text != lastCopied)
            {
                lastCopied = text;

                if (text == lastPasted)
                {
                    ClipboardList.Items.RemoveAt(lastIndex);
                }

                if (ClipboardList.Items.Count == 50)
                {
                    ClipboardList.Items.RemoveAt(49);
                }
            
                Dispatcher.Invoke(() => ClipboardList.Items.Insert(0, text));
            }
        }

        private void ClipboardList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ClipboardList.SelectedItem != null)
            {
                string selectedText = ClipboardList.SelectedItem.ToString() ?? string.Empty;
                lastPasted = selectedText;
                lastIndex = ClipboardList.SelectedIndex;
                Clipboard.SetText(selectedText); // Set the clipboard to the selected text
                
                // Hide your window
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
            ClipboardList.Items.Clear();
            lastCopied = string.Empty;
            lastPasted = string.Empty;
        }

        private void DraggableArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if the left mouse button is pressed to initiate the drag
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // DragMove allows the window to be dragged around with the mouse
                DragMove();
            }
        }
    }
}