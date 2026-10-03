using System;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class EditUserModal : UserControl
    {
        private readonly UserService _userService = new UserService();
        private readonly int _userId;

        public event Action? Saved;
        public event Action? Cancelled;

        public EditUserModal(int userId, string username, string role)
        {
            InitializeComponent();

            _userId = userId;

            AccessBox.ItemsSource = Enum.GetValues(typeof(AppRole));

            UsernameBox.Text = username;
            if (Enum.TryParse<AppRole>(role, out var parsedRole))
                AccessBox.SelectedItem = parsedRole;
            else
                AccessBox.SelectedItem = AppRole.User;

            Loaded += (s, e) =>
            {
                UsernameBox.Focus();
                UsernameBox.CaretIndex = UsernameBox.Text.Length;
            };
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
            if (!string.IsNullOrWhiteSpace(password) && password.Length < 4)
            {
                ShowError("Password must be at least 4 characters.");
                return;
            }

            try
            {
                if (_userService.UsernameExists(username, _userId))
                {
                    ShowError("That username is already taken.");
                    return;
                }

                _userService.UpdateUser(_userId, username, access, string.IsNullOrWhiteSpace(password) ? null : password);
                Saved?.Invoke();
            }
            catch (Exception ex)
            {
                ShowError("Could not save changes: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}