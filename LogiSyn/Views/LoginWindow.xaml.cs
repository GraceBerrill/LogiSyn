using System;
using System.Windows;
using System.Windows.Controls;
<<<<<<< HEAD
using LogiSyn.Model;
using LogiSyn.Services;
=======
using LogiSyn.Services;
using LogiSyn.Model;
using LogiSyn.Views;
>>>>>>> origin/Feature/desktop-admin-manage-products

namespace LogiSyn.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginService _loginService = new LoginService();

        private bool _passwordRevealed;
        private bool _syncing;

        public User? LoggedInUser { get; private set; }

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
            string password = _passwordRevealed ? PasswordRevealBox.Text : PasswordBox.Password;

            if (username.Length == 0 || password.Length == 0)
            {
                MessageBox.Show("Please enter your username and password.",
                                "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

<<<<<<< HEAD
            try
            {
                User? loggedInUser = _loginService.Authenticate(username, password);

                if (loggedInUser != null)
                {
                    LoggedInUser = loggedInUser;

                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.", "Login Failed",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
=======
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
>>>>>>> origin/Feature/desktop-admin-manage-products
        }
    }
}