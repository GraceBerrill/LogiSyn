using LogiSyn.Model;
using LogiSyn.Services;
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class LoginWindow : Window
    {
        private bool _passwordRevealed;
        private bool _syncing;
        private readonly LoginService _loginService = new LoginService();

        public LoginWindow()
        {
            InitializeComponent();

            // Spreads "ANDERSON'S BAKERY" out letter by letter (WPF has no letter-spacing property)
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

        // ---------- show / hide password (the eye icon) ----------

        private void ToggleReveal_Click(object sender, RoutedEventArgs e)
        {
            _passwordRevealed = !_passwordRevealed;

            PasswordBox.Visibility = _passwordRevealed ? Visibility.Collapsed : Visibility.Visible;
            PasswordRevealBox.Visibility = _passwordRevealed ? Visibility.Visible : Visibility.Collapsed;

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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (username.Length == 0 || password.Length == 0)
            {
                MessageBox.Show("Please enter your username and password.",
                                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User? user;
            try
            {
                user = _loginService.Authenticate(username, password);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not reach the database:\n" + ex.Message,
                                "Login error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (user == null)
            {
                MessageBox.Show("Invalid username or password.",
                                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Map the DB string role to the AppRole enum the shell expects.
            if (!Enum.TryParse<AppRole>(user.Role, out var role))
            {
                MessageBox.Show($"Unknown role '{user.Role}' for this account.",
                                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            new ShellWindow(role).Show();
            Close();
        }
    }
}