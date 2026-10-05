using AndersonsBakeryAPI.Services;
using SharedLibrary.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using LogiSyn.Views;

namespace LogiSyn.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginServiceRouter _loginService = App.ServiceProvider.GetService<LoginServiceRouter>() ?? new LoginServiceRouter();

        private bool _passwordRevealed;
        private bool _syncing;

        public UserRow? LoggedInUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            BrandLetters.ItemsSource = "ANDERSON'S BAKERY";
            UsernameBox.Focus();
        }

        // ---------- placeholders ----------

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsernamePlaceholder.Visibility =
                string.IsNullOrEmpty(UsernameBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;
            PasswordRevealBox.Text = PasswordBox.Password;
            _syncing = false;
            UpdatePasswordPlaceholder();
        }

        private void PasswordRevealBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;
            _syncing = true;
            PasswordBox.Password = PasswordRevealBox.Text;
            _syncing = false;
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            PasswordPlaceholder.Visibility =
                string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- show / hide password ----------

        private void ToggleReveal_Click(object sender, RoutedEventArgs e)
        {
            _passwordRevealed = !_passwordRevealed;
            PasswordBox.Visibility = _passwordRevealed ? Visibility.Collapsed : Visibility.Visible;
            PasswordRevealBox.Visibility = _passwordRevealed ? Visibility.Visible : Visibility.Collapsed;
            EyeIcon.Visibility = _passwordRevealed ? Visibility.Visible : Visibility.Collapsed;
            EyeOffIcon.Visibility = _passwordRevealed ? Visibility.Collapsed : Visibility.Visible;

            if (_passwordRevealed)
            {
                PasswordRevealBox.Focus();
                PasswordRevealBox.CaretIndex = PasswordRevealBox.Text.Length;
            }
            else
            {
                PasswordBox.Focus();
            }
        }

        // ---------- log in ----------

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = _passwordRevealed ? PasswordRevealBox.Text : PasswordBox.Password;

            if (username.Length == 0 || password.Length == 0)
            {
                MessageBox.Show("Please enter your username and password.",
                                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UserRow? user = null;

            LoginButton.IsEnabled = false;
            try
            {
                // First try the hosted API via ApiClient if available
                var api = App.ServiceProvider.GetService<ApiClient>() ?? new ApiClient();
                try
                {
                    // Await without ConfigureAwait so continuation runs on the UI thread
                    user = await api.AuthenticateAsync(username, password);
                }
                catch (Exception apiEx)
                {
                    // API failed; log and fall back to local auth
                    Console.WriteLine($"[Login] API auth failed: {apiEx.Message}");
                    user = null;
                }

                if (user == null)
                {
                    try
                    {
                        // Fallback to the local login router
                        user = _loginService.Authenticate(username, password);
                    }
                    catch (Exception localEx)
                    {
                        // Both methods failed - show an error
                        MessageBox.Show("Could not authenticate using API or local router:\n" + localEx.Message,
                                        "Login error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }

            if (user == null)
            {
                MessageBox.Show("Invalid username or password.",
                                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Enum.TryParse<AppRole>(user.Role, ignoreCase: true, out var appRole))
            {
                MessageBox.Show($"Unknown role '{user.Role}' for this account.",
                                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            user.Role = appRole.ToString();

            // Continuation is on UI thread, safe to set DialogResult
            LoggedInUser = user;
            DialogResult = true;
        }
    }
}
