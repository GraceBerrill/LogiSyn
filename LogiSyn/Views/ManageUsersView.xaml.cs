using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;

namespace LogiSyn.Views
{
    public partial class ManageUsersView : UserControl
    {
        private readonly List<UserRow> _all;
        private bool _ready;

        public ManageUsersView()
        {
            InitializeComponent();

            // TODO (backend): load the real users here
            _all = SampleData.Users();

            _ready = true;
            Refresh();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            UserList.ItemsSource = _all.Where(u =>
                q.Length == 0
                || u.Id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || u.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || u.Access.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: the Figma file has no "add user" form yet - open it here once it exists
            MessageBox.Show("The Add User form will be added later.", "Add Users");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open the edit form for this user
            MessageBox.Show("The Edit User form will be added later.", "Edit user");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var user = ((FrameworkElement)sender).DataContext as UserRow;
            if (user == null) return;

            var answer = MessageBox.Show("Delete " + user.Name + "?", "Delete user",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            // TODO (backend): delete the user in the database
            _all.Remove(user);
            Refresh();
        }
    }
}