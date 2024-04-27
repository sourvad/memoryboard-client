using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Memoryboard
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;
        private MainWindow _parentWindow;

        public LoginPage()
        {
            InitializeComponent();
            _httpClient = HttpClientSingleton.Instance;
            _options = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Loaded += OnLoaded;
        }

        public bool CanLogin
        {
            get
            {
                string email = EmailTextBox.Text;
                string password = PasswordBox.Password;

                return EmailValidator.IsEmailValid(email) && !string.IsNullOrEmpty(password); // Button enabled if email is valid and password is not empty
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this) as MainWindow;
        }


        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginMessage.Text = string.Empty;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            var loginRequest = new
            {
                Email = email,
                Password = password,
            };

            StringContent content = new(
                JsonSerializer.Serialize(loginRequest, _options),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await _httpClient.PostAsync("users/login", content);

            if (response.IsSuccessStatusCode)
            {
                string token = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    _parentWindow.StoreToken(token);
                    _parentWindow.NavigateTo(Pages.Clipboard);
                    _parentWindow.SetUserPassword(password);
                    _parentWindow.Initialize();
                }
                else
                {
                    LoginMessage.Text = "Invalid email or password";
                }
            }
            else
            {
                LoginMessage.Text = "Invalid email or password";
            }
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.NavigateTo(Pages.Register);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginButton.IsEnabled = CanLogin;
        }

        private void TextBox_PasswordChanged(object sender, RoutedEventArgs args)
        {
            LoginButton.IsEnabled = CanLogin;
        }
    }
}
