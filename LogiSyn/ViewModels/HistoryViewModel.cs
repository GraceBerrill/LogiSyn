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
    // ViewModel for completed orders history, handling date filtering and search queries.
    public class HistoryViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly OrderService _orderService;

        private List<OrderRow> _allHistory = new();
        private ObservableCollection<OrderRow> _filteredHistory = new();
        private string _searchQuery = string.Empty;
        private string _selectedDateFilter = "All";
        private string _dateText = DateTime.Now.ToString("dd MMMM yyyy");
        private string _dateHeader = "Date Completed";
        private bool _isLoading;

        public HistoryViewModel(ApiClient? apiClient = null, OrderService? orderService = null)
        {
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
            _orderService = orderService ?? App.ServiceProvider?.GetService<OrderService>() ?? new OrderService();
        }

        public ObservableCollection<OrderRow> FilteredHistory
        {
            get => _filteredHistory;
            set => SetProperty(ref _filteredHistory, value);
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

        public string SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                if (SetProperty(ref _selectedDateFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string DateText
        {
            get => _dateText;
            set => SetProperty(ref _dateText, value);
        }

        public string DateHeader
        {
            get => _dateHeader;
            set => SetProperty(ref _dateHeader, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public List<OrderRow> AllHistory => _allHistory;

        public void ConfigureRole(AppRole role)
        {
            DateHeader = role == AppRole.Admin ? "Date Completed" : "Date";
        }

        // Loads completed orders from the cloud API with fallback to local storage.
        public async Task LoadHistoryAsync()
        {
            IsLoading = true;
            try
            {
                List<OrderScaled>? orders = null;
                try
                {
                    orders = await _apiClient.GetOrdersAsync();
                }
                catch { }

                if (orders == null || !orders.Any())
                {
                    try
                    {
                        orders = (await _orderService.GetOrdersAsync())?.ToList() ?? new List<OrderScaled>();
                    }
                    catch
                    {
                        orders = new List<OrderScaled>();
                    }
                }

                _allHistory = orders
                    .Where(o => string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase))
                    .Select(o => OrderRow.FromOrderScaled(o))
                    .ToList();

                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Filters completed orders based on search query and date range.
        public void ApplyFilters()
        {
            string q = (_searchQuery ?? string.Empty).Trim();
            DateTime today = DateTime.Today;

            var filtered = _allHistory.Where(o =>
            {
                bool matchesSearch = q.Length == 0
                    || (o.Number ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || (o.Customer ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!matchesSearch) return false;

                return _selectedDateFilter switch
                {
                    "Today" => o.Date.Date == today,
                    "This Week" => o.Date.Date >= today.AddDays(-7),
                    "This Month" => o.Date.Date >= today.AddDays(-30),
                    _ => true
                };
            }).ToList();

            FilteredHistory = new ObservableCollection<OrderRow>(filtered);
        }
    }
}
