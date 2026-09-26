using LogiSyn.Model;
using LogiSyn.Services;
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class AddUserModal : UserControl
    {
        private readonly UserService _userService = new UserService();

        public AddUserModal()
        {
            InitializeComponent();
            AccessBox.SelectedIndex = 1; // default to "Staff"
            Loaded += (s, e) => UsernameBox.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string access = (AccessBox.SelectedItem as System.Windows.Controls.ComboBoxItem)
                            ?.Content?.ToString() ?? "Staff";

            // --- Validation ---
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Username is required.");
                return;
            }

            if (username.Length < 3)
            {
                ShowError("Username must be at least 3 characters.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Password is required.");
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Password must be at least 4 characters.");
                return;
            }

            try
            {
                if (_userService.UsernameExists(username))
                {
                    ShowError("That username is already taken.");
                    return;
                }

                _userService.AddUser(username, password, access);
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                ShowError("Could not save user: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}