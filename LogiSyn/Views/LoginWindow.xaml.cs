using System;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Services;
using LogiSyn.Model;
using LogiSyn.Views;

namespace LogiSyn.Views
{
    public partial class LoginWindow : Window
    {
        private bool _passwordRevealed;
        private bool _syncing;

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

            // Authenticate against the backend (SQL first, then local JSON fallback)
            var svc = new LoginService();
            var user = svc.Authenticate(username, password);

            if (user == null)
            {
                MessageBox.Show("Invalid username or password.", "Login", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Open main window and configure navigation/role
            var main = new global::LogiSyn.MainWindow();

            // Ensure ShellWindow controller exists (MainWindow ctor registers it)
            if (ShellWindow.Current != null)
            {
                // Map returned user role string to AppRole enum (case-insensitive)
                if (!Enum.TryParse<AppRole>(user.Role, true, out var roleEnum))
                    roleEnum = AppRole.User;

                ShellWindow.Current.Role = roleEnum;

                // Setup sidebar visibility and other role-based UI
                main.SetupSidebarNavigation(user.Role);
                // Display the logged-in username in the sidebar/profile
                main.SetProfileName(user.Username);

                // Load the dashboard configured for this role (User -> production staff)
                ShellWindow.Current.Navigate("dashboard");
            }

            main.Show();
            Close();
        }
    }
}