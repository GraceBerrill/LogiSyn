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
    // ViewModel for order management in the Admin/Manager view, handling order retrieval,
    // searching, status filtering, and live updates.
    public class AdminOrdersViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly OrderService _orderService;

        private List<OrderRow> _allOrders = new();
        private ObservableCollection<OrderRow> _filteredOrders = new();
        private string _searchQuery = string.Empty;
        private string _selectedStatusFilter = "All Statuses";
        private string _dateText = DateTime.Now.ToString("dd MMMM yyyy");
        private bool _isLoading;

        public AdminOrdersViewModel(ApiClient? apiClient = null, OrderService? orderService = null)
        {
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
            _orderService = orderService ?? App.ServiceProvider?.GetService<OrderService>() ?? new OrderService();
        }

        public ObservableCollection<OrderRow> FilteredOrders
        {
            get => _filteredOrders;
            set => SetProperty(ref _filteredOrders, value);
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

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
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

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public List<OrderRow> AllOrders => _allOrders;

        // Loads orders from the cloud API with fallback to local OrderService.
        public async Task LoadOrdersAsync()
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

                if (orders == null || orders.Count == 0)
                {
                    try
                    {
                        orders = (await _orderService.GetOrdersAsync()).ToList();
                    }
                    catch
                    {
                        orders = new List<OrderScaled>();
                    }
                }

                _allOrders = orders.Select(o => OrderRow.FromOrderScaled(o)).ToList();
                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Filters cached orders by search term and selected status.
        public void ApplyFilters()
        {
            string q = (_searchQuery ?? string.Empty).Trim();
            string status = _selectedStatusFilter ?? "All Statuses";

            var filtered = _allOrders.Where(o =>
                (q.Length == 0
                    || (o.Number ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || (o.Customer ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                && (status == "All Statuses" || string.Equals(o.Status, status, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            FilteredOrders = new ObservableCollection<OrderRow>(filtered);
        }
    }
}
