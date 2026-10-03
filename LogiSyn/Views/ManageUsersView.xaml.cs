using SharedLibrary.Model;
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
                _all = _userService.GetAllUsers();
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
                || (u.Role ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var modal = new AddUserModal();

            var host = new Window
            {
                Title = "Add User",
                Content = modal,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            modal.Saved += () => { host.DialogResult = true; host.Close(); };
            modal.Cancelled += () => { host.DialogResult = false; host.Close(); };

            if (host.ShowDialog() == true)
                LoadUsers();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var row = ((FrameworkElement)sender).DataContext as UserRow;
            if (row == null) return;

            int id;
            if (!int.TryParse(row.Id, out id))
            {
                MessageBox.Show("Could not determine user id.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var modal = new EditUserModal(id, row.Name, row.Role);

            var host = new Window
            {
                Title = "Edit User",
                Content = modal,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            modal.Saved += () => { host.DialogResult = true; host.Close(); };
            modal.Cancelled += () => { host.DialogResult = false; host.Close(); };

            if (host.ShowDialog() == true)
                LoadUsers();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var row = ((FrameworkElement)sender).DataContext as UserRow;
            if (row == null) return;

            var answer = MessageBox.Show("Delete " + row.Name + "?", "Delete user",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            int id;
            if (!int.TryParse(row.Id, out id))
            {
                MessageBox.Show("Could not determine user id.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _userService.DeleteUser(id);
                _all.Remove(row);
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