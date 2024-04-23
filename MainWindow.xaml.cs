using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Memoryboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private readonly ClipboardPage _clipboardPage;
        private readonly LoginPage _loginPage;
        private readonly RegisterPage _registerPage;

        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int MOD_WIN = 0x0008;
        private const int VK_V = 0x56;
        private const int WM_HOTKEY = 0x0312;

        private readonly string _tokenFilePath;
        private string _token;


        private ClipboardSignalRClient _signalRClient;

        private IntPtr windowHandle;

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

            _tokenFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Memoryboard", "userToken.txt");
            _clipboardPage = new ClipboardPage();
            _loginPage = new LoginPage();
            _registerPage = new RegisterPage();

            if (IsUserLoggedIn())
            {
                ContentPlaceholder.Navigate(_clipboardPage);
            }
            else
            {
                ContentPlaceholder.Navigate(_loginPage);
            }

            _httpClient = HttpClientSingleton.Instance;
        }

        public async void StoreToken(string token)
        {
            string directoryPath = Path.GetDirectoryName(_tokenFilePath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath); 
            }

            File.WriteAllText(_tokenFilePath, token);
            _token = token;
            
            if (_signalRClient != null)
            {
                await DisposeSignalRClient();
            }

            await GetUserClipboard();
            await InitSignalRClient();
        }

        public void NavigateTo(Pages page)
        {
            switch (page)
            {
                case Pages.Clipboard:
                    ContentPlaceholder.Navigate(_clipboardPage);
                    break;
                case Pages.Login:
                    ContentPlaceholder.Navigate(_loginPage);
                    break;
                case Pages.Register:
                    ContentPlaceholder.Navigate(_registerPage);
                    break;
            }
        }

        public async void Logout()
        {
            await DisposeSignalRClient();
            DeleteToken();
            ContentPlaceholder.Navigate(_loginPage);
        }

        public void DeleteToken()
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
                _token = string.Empty;
            }
        }

        public void SendCopyEvent(string copiedText)
        {
            _signalRClient.SendCopyEvent(copiedText);
        }

        public void SendSelectEvent(int selectedIndex)
        {
            _signalRClient.SendSelectEvent(selectedIndex);
        }

        public void SendClearAllEvent()
        {
            _signalRClient.SendClearAllEvent();
        }

        private async Task GetUserClipboard()
        {

            HttpRequestMessage request = new(HttpMethod.Get, "clipboard");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                List<string> clipboardItems = JsonSerializer.Deserialize<List<string>>(responseBody);

                _clipboardPage.PopulateClipboard(clipboardItems);
            }
        }

        private async Task InitSignalRClient()
        {
            _signalRClient = new ClipboardSignalRClient("https://localhost:7193/hubs/clipboard", _token);
            _signalRClient.BroadcastCopyReceived += OnBroadcastCopyReceived;
            _signalRClient.BroadcastSelectReceived += OnBroadcastSelectReceived;
            _signalRClient.BroadcastClearAllReceived += OnBroadcastClearAllReceived;
            await _signalRClient.ConnectAsync();
        }

        private async Task DisposeSignalRClient()
        {
            await _signalRClient.DisconnectAsync();
            _signalRClient = null;
        }

        private void OnBroadcastCopyReceived(string copiedText)
        {
            _clipboardPage.BroadcastCopyReceived(copiedText);
        }

        private void OnBroadcastSelectReceived(int selectedIndex)
        {
            _clipboardPage.BroadcastSelectReceived(selectedIndex);
        }

        private void OnBroadcastClearAllReceived()
        {
            _clipboardPage.BroadcastClearAllReceived();
        }

        private bool IsUserLoggedIn()
        {
            if (File.Exists(_tokenFilePath))
            {
                string storedToken = File.ReadAllText(_tokenFilePath);

                if (!string.IsNullOrEmpty(storedToken))
                {
                    bool isValidToken = JwtTokenHandler.IsTokenExpired(storedToken);

                    if (isValidToken)
                    {
                        _token = storedToken;
                        return true;
                    }
                    else
                    {
                        DeleteToken();
                    }
                }
            }

            return false;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(windowHandle)?.AddHook(WndProc);

            var hWnd = windowHandle;
            RegisterHotKey(hWnd, 0, MOD_WIN, VK_V); // ID for the hotkey is 0
            ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);

            // Start listening to clipboard changes
            AddClipboardFormatListener(windowHandle);

            if (IsUserLoggedIn())
            {
                await InitSignalRClient();
                await GetUserClipboard();
            }

            Hide();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Stop listening to clipboard changes
            RemoveClipboardFormatListener(windowHandle);
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            // Hide your window
            Hide();
        }
        private async void OnClosed(object sender, EventArgs e)
        {
            // Unregister the hotkey
            IntPtr handle = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(handle, 0);
            await DisposeSignalRClient();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                _clipboardPage.OnClipboardChanged();
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