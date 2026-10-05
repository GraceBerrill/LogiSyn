using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class ManageUsersView : UserControl
    {
        private readonly UserServiceRouter _userService = App.ServiceProvider.GetService<UserServiceRouter>() ?? new UserServiceRouter();
        private readonly AndersonsBakeryAPI.Services.ApiClient _apiClient = App.ServiceProvider.GetService<AndersonsBakeryAPI.Services.ApiClient>() ?? new AndersonsBakeryAPI.Services.ApiClient();
        private readonly SyncService _syncService = new SyncService();
        private List<UserRow> _all = new();
        private bool _ready;
        private bool _syncing;

        public ManageUsersView()
        {
            InitializeComponent();
            _ready = true;

            Loaded += async (s, e) => await LoadUsersAsync();
        }

        private async System.Threading.Tasks.Task LoadUsersAsync()
        {
            LoadingText.Visibility = System.Windows.Visibility.Visible;

            try
            {
                // Try API first, fall back to local router
                try
                {
                    var users = await _apiClient.GetUsersAsync();
                    _all = users;
                }
                catch
                {
                    _all = _userService.GetAllUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load users: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _all = new List<UserRow>();
            }
            finally
            {
                LoadingText.Visibility = System.Windows.Visibility.Collapsed;
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


        private async void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            await RunSyncAsync(silentWhenNothingToDo: false);
        }

        private async Task RunSyncAsync(bool silentWhenNothingToDo)
        {
            if (_syncing) return;
            _syncing = true;

            SetSyncUiBusy(true);

            try
            {
                var result = await Task.Run(() => _syncService.SyncUsers());

                if (result.SqlToMongo == 0 && result.Skipped == 0 && result.Failed == 0)
                {
                    if (!silentWhenNothingToDo)
                    {
                        MessageBox.Show("Everything is already in sync.",
                                        "Sync", MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }
                    return;
                }

                var icon = result.Failed > 0 ? MessageBoxImage.Warning
                         : MessageBoxImage.Information;

                MessageBox.Show(result.ToString(),
                                result.Failed > 0 ? "Sync completed with errors" : "Sync complete",
                                MessageBoxButton.OK, icon);

                if (result.SqlToMongo > 0)
                    await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync failed:\n" + ex.Message,
                                "Sync error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetSyncUiBusy(false);
                _syncing = false;
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
                await LoadUsersAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var row = ((FrameworkElement)sender).DataContext as UserRow;
            if (row == null) return;

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
                await LoadUsersAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var row = ((FrameworkElement)sender).DataContext as UserRow;
            if (row == null) return;

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
                // Try remote API delete first, fallback to local delete
                var deleted = false;
                try
                {
                    deleted = await _apiClient.DeleteUserAsync(identifier);
                }
                catch { }

                if (!deleted)
                {
                    _userService.DeleteUser(identifier);
                }

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