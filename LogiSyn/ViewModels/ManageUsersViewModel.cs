using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Model;

namespace LogiSyn.ViewModels
{
    // ViewModel for managing user accounts, searching, and cloud/local synchronization.
    public class ManageUsersViewModel : ViewModelBase
    {
        private readonly UserServiceRouter _userService;
        private readonly ApiClient _apiClient;
        private readonly SyncService _syncService;

        private List<UserRow> _allUsers = new();
        private ObservableCollection<UserRow> _filteredUsers = new();
        private string _searchQuery = string.Empty;
        private bool _isLoading;
        private bool _isSyncing;

        public ManageUsersViewModel(
            UserServiceRouter? userService = null,
            ApiClient? apiClient = null,
            SyncService? syncService = null)
        {
            _userService = userService ?? App.ServiceProvider?.GetService<UserServiceRouter>() ?? new UserServiceRouter();
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
            _syncService = syncService ?? new SyncService();
        }

        public ObservableCollection<UserRow> FilteredUsers
        {
            get => _filteredUsers;
            set => SetProperty(ref _filteredUsers, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    ApplyFilters();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        public List<UserRow> AllUsers => _allUsers;

        // Loads users from the cloud API with fallback to local user router.
        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            try
            {
                try
                {
                    var users = await _apiClient.GetUsersAsync();
                    _allUsers = users ?? new List<UserRow>();
                }
                catch
                {
                    _allUsers = _userService.GetAllUsers() ?? new List<UserRow>();
                }

                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Applies search filter on cached users.
        public void ApplyFilters()
        {
            string q = (_searchQuery ?? string.Empty).Trim();
            var filtered = _allUsers.Where(u =>
                q.Length == 0
                || (u.Id ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (u.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (u.Role ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();

            FilteredUsers = new ObservableCollection<UserRow>(filtered);
        }

        // Triggers full sync of users between local SQL and MongoDB Atlas.
        public async Task<SyncResult> RunSyncAsync()
        {
            IsSyncing = true;
            try
            {
                var result = await Task.Run(() => _syncService.SyncUsers());
                if (result.SqlToMongo > 0)
                {
                    await LoadUsersAsync();
                }
                return result;
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }
}
