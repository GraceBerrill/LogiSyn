using System;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class AddUserModal : UserControl
    {
        private readonly UserService _userService = new UserService();

        public event Action? Saved;
        public event Action? Cancelled;

        public AddUserModal()
        {
            InitializeComponent();

            AccessBox.ItemsSource = Enum.GetValues(typeof(AppRole));

            AccessBox.SelectedItem = AppRole.User;

            Loaded += (s, e) => UsernameBox.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            string access = AccessBox.SelectedItem?.ToString() ?? nameof(AppRole.User);

            if (string.IsNullOrWhiteSpace(username)) { ShowError("Username is required."); return; }
            if (username.Length < 3) { ShowError("Username must be at least 3 characters."); return; }
            if (string.IsNullOrWhiteSpace(password)) { ShowError("Password is required."); return; }
            if (password.Length < 4) { ShowError("Password must be at least 4 characters."); return; }

            try
            {
                if (_userService.UsernameExists(username))
                {
                    ShowError("That username is already taken.");
                    return;
                }

                _userService.AddUser(username, password, access);
                Saved?.Invoke();
            }
            catch (Exception ex)
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