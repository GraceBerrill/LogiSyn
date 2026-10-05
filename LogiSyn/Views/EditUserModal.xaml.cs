using System;
using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LogiSyn.Views
{
    public partial class EditUserModal : UserControl
    {
        private readonly UserServiceRouter _userService = App.ServiceProvider.GetService<UserServiceRouter>() ?? new UserServiceRouter();
        private readonly string _mongoId;

        public event Action? Saved;
        public event Action? Cancelled;

        public EditUserModal(string mongoId, string username, string role)
        {
            InitializeComponent();

            _mongoId = mongoId;

            AccessBox.ItemsSource = Enum.GetValues(typeof(AppRole));

            UsernameBox.Text = username;
            if (Enum.TryParse<AppRole>(role, ignoreCase: true, out var parsedRole))
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
                var api = App.ServiceProvider.GetService<ApiClient>() ?? new ApiClient();
                // Try to update via API (if we have a MongoId / API record)
                if (!string.IsNullOrWhiteSpace(_mongoId))
                {
                    var user = new UserRow { Id = _mongoId, Name = username, Role = access };
                    if (!string.IsNullOrWhiteSpace(password)) user.Password = password;
                    var updated = api.UpdateUserAsync(_mongoId, user).GetAwaiter().GetResult();
                    if (updated)
                    {
                        Saved?.Invoke();
                        return;
                    }
                }

                // Exclude the current user from the "already taken" check by MongoId
                if (_userService.UsernameExists(username, _mongoId))
                {
                    ShowError("That username is already taken.");
                    return;
                }

                _userService.UpdateUser(
                    _mongoId,
                    username,
                    access,
                    string.IsNullOrWhiteSpace(password) ? null : password);

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