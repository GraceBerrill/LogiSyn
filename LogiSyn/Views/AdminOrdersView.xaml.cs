using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class AdminOrdersView : UserControl
    {
        private List<OrderRow> _all = new();
        private bool _ready;
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();
        private DispatcherTimer _refreshTimer;

        public AdminOrdersView()
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();

            StatusFilter.SelectedIndex = 0;
            _ready = true;

            // Load orders on initialization
            LoadOrdersAsync();

            // Set up auto-refresh timer (refresh every 10 seconds)
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(10);
            _refreshTimer.Tick += (s, e) => LoadOrdersAsync();
            _refreshTimer.Start();
        }

        /// <summary>
        /// Loads orders from API or local OrderService
        /// </summary>
        private async void LoadOrdersAsync()
        {
            try
            {
                // Attempt to fetch orders from API
                var orders = await _apiClient.GetOrdersAsync();

                // If API returned no orders (e.g. API down or empty), fallback to local OrderService
                if (orders.Count == 0)
                {
                    orders = _orderService.GetOrders().ToList();
                }

                // Convert OrderScaled to OrderRow for UI display
                _all = orders
                    .Select(o => OrderRow.FromOrderScaled(o))
                    .ToList();

                Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading orders: {ex.Message}");
                var localOrders = _orderService.GetOrders().ToList();
                if (localOrders.Count > 0)
                {
                    _all = localOrders.Select(o => OrderRow.FromOrderScaled(o)).ToList();
                }
                else if (_all.Count == 0)
                {
                    _all = SampleData.Orders();
                }
                Refresh();
            }
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = StatusFilter.SelectedItem as ComboBoxItem;
            string status = selected == null ? "All Statuses" : (string)selected.Content;

            OrderList.ItemsSource = _all.Where(o =>
                (q.Length == 0
                    || o.Number.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || o.Customer.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                && (status == "All Statuses" || o.Status == status)).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void LinkOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            if (_all.Count > 0)
            {
                ShellWindow.Current?.Navigate("breakdown", _all[0]);
            }
            else
            {
                ShellWindow.Current?.Navigate("createorder");
            }
        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShellWindow.Current != null)
            {
                ShellWindow.Current.Navigate("createorder");
                return;
            }

            var parentWindow = Window.GetWindow(this);
            if (parentWindow?.FindName("MainContent") is ContentControl mainContent)
            {
                mainContent.Content = new CreateOrderView();
                return;
            }

            MessageBox.Show($"Could not find navigation container. Active window is: {parentWindow?.GetType().Name}");
        }

        /// <summary>
        /// Refresh orders manually (can be called from a refresh button if added to UI)
        /// </summary>
        public void RefreshOrders()
        {
            LoadOrdersAsync();
        }
    }
}
