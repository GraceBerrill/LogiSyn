using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.ViewModels;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    // Interaction logic for ManageUsersView.xaml.
    // User account operations, searching, and cloud sync are handled by ManageUsersViewModel.
    public partial class ManageUsersView : UserControl
    {
        public ManageUsersViewModel ViewModel { get; }

        public ManageUsersView()
        {
            InitializeComponent();

            ViewModel = new ManageUsersViewModel();
            DataContext = ViewModel;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ManageUsersViewModel.FilteredUsers))
                {
                    UserList.ItemsSource = ViewModel.FilteredUsers;
                }
                if (e.PropertyName == nameof(ManageUsersViewModel.IsLoading))
                {
                    LoadingText.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                }
                if (e.PropertyName == nameof(ManageUsersViewModel.IsSyncing))
                {
                    SetSyncUiBusy(ViewModel.IsSyncing);
                }
            };

            Loaded += async (s, e) => await ViewModel.LoadUsersAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchQuery = SearchBox.Text.Trim();
        }

        private async void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ViewModel.RunSyncAsync();
                if (result.SqlToMongo == 0 && result.Skipped == 0 && result.Failed == 0)
                {
                    MessageBox.Show("Everything is already in sync.", "Sync", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var icon = result.Failed > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information;
                MessageBox.Show(result.ToString(), result.Failed > 0 ? "Sync completed with errors" : "Sync complete",
                                MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync failed:\n" + ex.Message, "Sync error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetSyncUiBusy(bool busy)
        {
            SyncButton.IsEnabled = !busy;
            AddUserButton.IsEnabled = !busy;
            SyncButtonText.Text = busy ? "Syncing…" : "Sync";
        }

        private async void AddUserButton_Click(object sender, RoutedEventArgs e)
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
                await ViewModel.LoadUsersAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not UserRow row) return;

            string identifier = !string.IsNullOrEmpty(row.Id) ? row.Id : row.SqlId;
            if (string.IsNullOrEmpty(identifier))
            {
                MessageBox.Show("Unable to identify user record.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var modal = new EditUserModal(identifier, row.Name, row.Role);

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
                await ViewModel.LoadUsersAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not UserRow row) return;

            string identifier = !string.IsNullOrEmpty(row.Id) ? row.Id : row.SqlId;
            if (string.IsNullOrEmpty(identifier))
            {
                MessageBox.Show("Unable to identify user record.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var answer = MessageBox.Show("Delete " + row.Name + "?", "Delete user",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                var client = new AndersonsBakeryAPI.Services.ApiClient();
                var deleted = false;
                try
                {
                    deleted = await client.DeleteUserAsync(identifier);
                }
                catch { }

                if (!deleted)
                {
                    var localRouter = new AndersonsBakeryAPI.Services.UserServiceRouter();
                    localRouter.DeleteUser(identifier);
                }

                await ViewModel.LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
