using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginService _loginService = new LoginService();

        private bool _passwordRevealed;
        private bool _syncing;

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

            try
            {
                User? loggedInUser = _loginService.Authenticate(username, password);

                if (loggedInUser != null)
                {
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
        }
    }
}