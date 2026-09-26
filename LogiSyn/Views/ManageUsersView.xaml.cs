using LogiSyn.Model;
using LogiSyn.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class ManageUsersView : UserControl
    {
        private readonly UserService _userService = new UserService();
        private List<UserRow> _all = new();
        private bool _ready;

        public ManageUsersView()
        {
            InitializeComponent();
            _ready = true;
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                _all = _userService.GetAllUsers()
                    .Select(u => new UserRow
                    {
                        UserId = u.Id,
                        Id = "USR-" + u.Id.ToString("D4"),
                        Name = u.Username,
                        Access = u.Role,
                        DateAdded = DateTime.Now.ToString("yyyy-MM-dd") // or add a DateAdded column in DB
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load users: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _all = new List<UserRow>();
            }

            Refresh();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            UserList.ItemsSource = _all.Where(u =>
                q.Length == 0
                || (u.Id ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (u.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (u.Access ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddUserDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                LoadUsers(); // reload from DB so the new user appears
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("The Edit User form will be added later.", "Edit user");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var user = ((FrameworkElement)sender).DataContext as UserRow;
            if (user == null) return;

            var answer = MessageBox.Show("Delete " + user.Name + "?", "Delete user",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                _userService.DeleteUser(user.UserId);
                _all.Remove(user);
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}