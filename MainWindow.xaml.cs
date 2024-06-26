﻿using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Memoryboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int MOD_SHIFT = 0x4; 
        private const int MOD_ALT = 0x1;   
        private const int VK_Z = 0x5A;     
        private const int WM_HOTKEY = 0x0312;

        private readonly HttpClient _httpClient;
        private readonly ClipboardPage _clipboardPage;
        private readonly LoginPage _loginPage;
        private readonly RegisterPage _registerPage;
        private readonly string _tokenFilePath;
        private readonly string _passwordFilePath;

        private ClipboardSignalRClient _signalRClient;
        private byte[] _userPasswordBytes;
        private string _token;
        private nint _windowHandle;
        private bool _isHookAdded;

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
            _passwordFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Memoryboard", "userPassword.txt");
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
            _isHookAdded = false;
        }

        public void StoreToken(string token)
        {
            string directoryPath = Path.GetDirectoryName(_tokenFilePath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(_tokenFilePath, token);
            _token = token;
        }

        public async void Initialize()
        {
            if (_signalRClient != null)
            {
                await DisposeSignalRClient();
            }

            await InitSignalRClient();
            await GetUserClipboard();

            if (!_isHookAdded)
            {
                _isHookAdded = true;
                HwndSource.FromHwnd(_windowHandle)?.AddHook(WndProc);
            }
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
            if (_isHookAdded)
            {
                _isHookAdded = false;
                HwndSource.FromHwnd(_windowHandle)?.RemoveHook(WndProc);
            }

            await DisposeSignalRClient();
            DeletePassword();
            DeleteToken();
            ContentPlaceholder.Navigate(_loginPage);
        }

        private void DeleteToken()
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }

            _token = string.Empty;
        }

        private void DeletePassword()
        {
            if (File.Exists(_passwordFilePath))
            {
                File.Delete(_passwordFilePath);
            }

            _userPasswordBytes = default;
        }

        public void SendCopyEvent(string copiedText)
        {
            _signalRClient.SendCopyEvent(AESHelper.Encrypt(copiedText, _userPasswordBytes));
        }

        public void SendSelectEvent(int selectedIndex)
        {
            _signalRClient.SendSelectEvent(selectedIndex);
        }

        public void SendClearAllEvent()
        {
            _signalRClient.SendClearAllEvent();
        }

        public void SetUserPassword(string password)
        {
            _userPasswordBytes = Encoding.UTF8.GetBytes(password);

            string directoryPath = Path.GetDirectoryName(_passwordFilePath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(_passwordFilePath, password);
        }

        private async Task GetUserClipboard()
        {

            HttpRequestMessage request = new(HttpMethod.Get, "clipboard");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                List<byte[]> clipboardItemsEncrypted = JsonSerializer.Deserialize<List<byte[]>>(responseBody);
                List<string> clipboardItems = clipboardItemsEncrypted
                    .Select(encryptedBytes => AESHelper.Decrypt(encryptedBytes, _userPasswordBytes))
                    .ToList();

                _clipboardPage.PopulateClipboard(clipboardItems);
            }
        }

        private async Task InitSignalRClient()
        {
            _signalRClient = new ClipboardSignalRClient("https://memoryboard-api.fly.dev/hubs/clipboard", _token);
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

        private void OnBroadcastCopyReceived(byte[] encryptedBytes)
        {
            _clipboardPage.BroadcastCopyReceived(AESHelper.Decrypt(encryptedBytes, _userPasswordBytes));
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
            bool isValidToken = false;
            bool isValidPassword = false;

            if (File.Exists(_tokenFilePath))
            {
                string storedToken = File.ReadAllText(_tokenFilePath);

                if (!string.IsNullOrEmpty(storedToken))
                {
                    isValidToken = JwtTokenHandler.IsTokenExpired(storedToken);

                    if (isValidToken)
                    {
                        _token = storedToken;
                    }
                    else
                    {
                        DeletePassword();
                        DeleteToken();
                    }
                }
            }

            if (File.Exists(_passwordFilePath))
            {
                string storedPassword = File.ReadAllText(_passwordFilePath);

                if (!string.IsNullOrEmpty(storedPassword))
                {
                    _userPasswordBytes = Encoding.UTF8.GetBytes(storedPassword);
                    isValidPassword = true;
                }
                else
                {
                    DeletePassword();
                    DeleteToken();
                }
            }

            return isValidPassword && isValidToken;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;

            RegisterHotKey(_windowHandle, 0, MOD_SHIFT | MOD_ALT, VK_Z); // ID for the hotkey is 0
            ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);

            // Start listening to clipboard changes
            AddClipboardFormatListener(_windowHandle);
            
            if (IsUserLoggedIn())
            {
                await InitSignalRClient();
                await GetUserClipboard();

                if (!_isHookAdded)
                {
                    _isHookAdded = true;
                    HwndSource.FromHwnd(_windowHandle)?.AddHook(WndProc);
                }
            }

            Hide();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Stop listening to clipboard changes
            RemoveClipboardFormatListener(_windowHandle);
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            // Hide your window
            Hide();
        }
        private async void OnClosed(object sender, EventArgs e)
        {

            // Unregister the hotkey
            if (_isHookAdded)
            {
                _isHookAdded = false;
                HwndSource.FromHwnd(_windowHandle)?.RemoveHook(WndProc);
            }

            RemoveClipboardFormatListener(_windowHandle);
            UnregisterHotKey(_windowHandle, 0);
            await DisposeSignalRClient();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                handled = true;
                _clipboardPage.OnClipboardChanged();
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