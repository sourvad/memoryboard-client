using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Memoryboard
{
    /// <summary>
    /// Interaction logic for RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;
        private MainWindow _parentWindow;

        public RegisterPage()
        {
            InitializeComponent();
            _parentWindow = Window.GetWindow(this) as MainWindow;
            _httpClient = HttpClientSingleton.Instance;
            _options = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Loaded += OnLoaded;
        }

        public bool CanRegister
        {
            get
            {
                bool email = IsEmailValid();
                bool password = IsValidPassword();

                return email && password;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this) as MainWindow;
        }

        private bool IsEmailValid()
        {
            string email = EmailTextBox.Text;
            bool isValidEmail = EmailValidator.IsEmailValid(email);

            if (isValidEmail)
            {
                EmailErrorMessage.Text = string.Empty;
                return isValidEmail;
            }
            else
            {
                if (!string.IsNullOrEmpty(email))
                {
                    EmailErrorMessage.Text = "Invalid email address";
                }
            }

            return isValidEmail;
        }

        private bool IsValidPassword()
        {
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 8 || password.Length > 32)
                {
                    PasswordErrorMessage.Text = "Minimum 8 and maximum 32 characters";
                }
                else
                {
                    PasswordErrorMessage.Text = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(password))
            {
                if (password != confirmPassword)
                {
                    ConfirmPasswordErrorMessage.Text = "Passwords do not match";
                }
                else
                {
                    ConfirmPasswordErrorMessage.Text = string.Empty;
                }
            }

            return !string.IsNullOrEmpty(password) &&
            password.Length >= 8 && password.Length <= 32 &&
            password == confirmPassword;
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterMessage.Text = string.Empty;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            var registerRequest = new
            {
                Email = email,
                Password = password,
            };

            StringContent content = new(
                JsonSerializer.Serialize(registerRequest, _options),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await _httpClient.PostAsync("users/register", content);

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
                    RegisterMessage.Text = "Invalid email or password";
                }
            }
            else
            {
                string responseMessage = await response.Content.ReadAsStringAsync();

                if (responseMessage == "User already exists")
                {
                    RegisterMessage.Text = "Account exists, please login instead";
                }
                else
                {
                    RegisterMessage.Text = "Unable to register, plaese try again";
                }
            }
        }

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.NavigateTo(Pages.Login);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegisterButton.IsEnabled = CanRegister;
        }

        private void TextBox_PasswordChanged(object sender, RoutedEventArgs args)
        {
            RegisterButton.IsEnabled = CanRegister;
        }
    }
}
