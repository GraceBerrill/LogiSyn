using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Model;

namespace LogiSyn.ViewModels
{
    // ViewModel for the main Dashboard view, handling order statistics, recent activity,
    // and multi-tier failover retrieval (Cloud API -> Local DB).
    public class DashboardViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly OrderService _orderService;

        private string _dateText = DateTime.Now.ToString("dd/MM/yyyy");
        private string _newOrdersLabel = "New Orders:";
        private string _newOrdersValue = "0";
        private string _completedValue = "0";
        private Visibility _exportButtonsVisibility = Visibility.Collapsed;
        private Visibility _noOrdersVisibility = Visibility.Collapsed;
        private bool _isLoading;
        private List<OrderScaled> _currentOrdersScaled = new();
        private ObservableCollection<DashboardOrderItem> _recentOrders = new();

        public DashboardViewModel(ApiClient? apiClient = null, OrderService? orderService = null)
        {
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
            _orderService = orderService ?? App.ServiceProvider?.GetService<OrderService>() ?? new OrderService();
        }

        public string DateText
        {
            get => _dateText;
            set => SetProperty(ref _dateText, value);
        }

        public string NewOrdersLabel
        {
            get => _newOrdersLabel;
            set => SetProperty(ref _newOrdersLabel, value);
        }

        public string NewOrdersValue
        {
            get => _newOrdersValue;
            set => SetProperty(ref _newOrdersValue, value);
        }

        public string CompletedValue
        {
            get => _completedValue;
            set => SetProperty(ref _completedValue, value);
        }

        public Visibility ExportButtonsVisibility
        {
            get => _exportButtonsVisibility;
            set => SetProperty(ref _exportButtonsVisibility, value);
        }

        public Visibility NoOrdersVisibility
        {
            get => _noOrdersVisibility;
            set => SetProperty(ref _noOrdersVisibility, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public List<OrderScaled> CurrentOrdersScaled
        {
            get => _currentOrdersScaled;
            private set => SetProperty(ref _currentOrdersScaled, value);
        }

        public ObservableCollection<DashboardOrderItem> RecentOrders
        {
            get => _recentOrders;
            set => SetProperty(ref _recentOrders, value);
        }

        // Configures UI visibility and labels based on user role.
        public void ConfigureRole(string role)
        {
            DateText = DateTime.Now.ToString("dd/MM/yyyy");

            switch (role?.Trim().ToLowerInvariant())
            {
                case "admin":
                    ExportButtonsVisibility = Visibility.Visible;
                    NewOrdersLabel = "New Orders:";
                    break;

                case "manager":
                    ExportButtonsVisibility = Visibility.Collapsed;
                    NewOrdersLabel = "On Going Orders:";
                    break;

                case "user":
                default:
                    ExportButtonsVisibility = Visibility.Collapsed;
                    NewOrdersLabel = "New Orders:";
                    break;
            }

            _ = LoadDashboardDataAsync();
        }

        // Loads dashboard statistics and recent orders from the cloud API with local DB fallback.
        public async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            try
            {
                CurrentOrdersScaled = await GetOrdersScaledAsync();

                NewOrdersValue = CurrentOrdersScaled.Count(o =>
                    string.Equals(o.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(o.Status, "In Production", StringComparison.OrdinalIgnoreCase)).ToString();

                CompletedValue = CurrentOrdersScaled.Count(o =>
                    string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToString();

                var items = CurrentOrdersScaled.Select(o =>
                {
                    bool isComp = string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase);

                    return new DashboardOrderItem
                    {
                        Number = o.OrderId,
                        Customer = o.Customer,
                        Status = o.Status,
                        DashStatusBrush = isComp
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8DBE98"))
                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C29D70"))
                    };
                }).ToList();

                RecentOrders = new ObservableCollection<DashboardOrderItem>(items);
                NoOrdersVisibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Retrieves orders from cloud API with seamless fallback to local storage.
        public async Task<List<OrderScaled>> GetOrdersScaledAsync()
        {
            List<OrderScaled> sourceOrders = new();
            try
            {
                sourceOrders = await _apiClient.GetOrdersAsync();
            }
            catch { }

            if (sourceOrders == null || sourceOrders.Count == 0)
            {
                try
                {
                    sourceOrders = (await _orderService.GetOrdersAsync()).ToList();
                }
                catch { }
            }

            return sourceOrders ?? new List<OrderScaled>();
        }

        // Resolves an OrderRow representation for navigating to the breakdown view.
        public OrderRow GetOrderRowForItem(DashboardOrderItem item)
        {
            var orderScaled = CurrentOrdersScaled.FirstOrDefault(o =>
                o.OrderId.Equals(item.Number, StringComparison.OrdinalIgnoreCase));

            return orderScaled != null
                ? OrderRow.FromOrderScaled(orderScaled)
                : new OrderRow
                {
                    Number = item.Number,
                    Customer = item.Customer,
                    Status = item.Status,
                    Date = DateTime.Now
                };
        }
    }

    // Display model for an order row in the recent orders section.
    public class DashboardOrderItem
    {
        public string Number { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Brush DashStatusBrush { get; set; } = Brushes.Gray;
    }
}
