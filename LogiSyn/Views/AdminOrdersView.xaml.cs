// Adriaan
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;
using System.Windows.Threading;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.Logging;

namespace LogiSyn.Views
{
    public partial class AdminOrdersView : UserControl
    {
        private List<OrderRow> _all = new();
        private bool _ready;
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();
        private DispatcherTimer _refreshTimer;

        //------------------------------------------------------------------------------------------------//

        // constructor
        public AdminOrdersView()
        {
            InitializeComponent();

            DateText.Text = DateTime.Now.ToString("dd MMMM yyyy");

            StatusFilter.SelectedIndex = 0;
            _ready = true;

            LoadOrdersAsync();

            // Set up auto-refresh timer so that orders are refreshed every 10 seconds
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(10);
            _refreshTimer.Tick += (s, e) => LoadOrdersAsync();
            _refreshTimer.Start();

            // Stop the timer once this view is navigated away from
            Unloaded += (s, e) => _refreshTimer.Stop();
        }

        //------------------------------------------------------------------------------------------------//

        // Load orders asynchronously from the API or local OrderService
        private async void LoadOrdersAsync()
        {
            try
            {
                // Attempt to fetch orders from API
                var orders = await _apiClient.GetOrdersAsync();

                // If API returned no orders (e.g. API down or empty), fallback to local OrderService
                if (orders == null || orders.Count == 0)
                {
                    try { orders = (await _orderService.GetOrdersAsync()).ToList(); }
                    catch { orders = new System.Collections.Generic.List<OrderScaled>(); }
                }

                // Convert OrderScaled to OrderRow for UI display
                _all = orders
                    .Select(o => OrderRow.FromOrderScaled(o))
                    .ToList();

                Refresh();
            }
            catch (Exception ex)
            {
                var logger = App.ServiceProvider?.GetService(typeof(Microsoft.Extensions.Logging.ILogger<AdminOrdersView>)) as Microsoft.Extensions.Logging.ILogger<AdminOrdersView>;
                logger?.LogWarning(ex, "Error loading orders");
                var localOrders = (await _orderService.GetOrdersAsync()).ToList();
                if (localOrders.Count > 0)
                {
                    _all = localOrders.Select(o => OrderRow.FromOrderScaled(o)).ToList();
                }
                Refresh();
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Refresh the displayed list of orders based on search query and status filter
        private void Refresh()
        {
            if (!_ready) return;

            // Get the search query and selected status filter
            string q = SearchBox.Text.Trim();
            var selected = StatusFilter.SelectedItem as ComboBoxItem;
            string status = selected == null ? "All Statuses" : (string)selected.Content;

            var filtered = _all.Where(o =>
                (q.Length == 0
                    || o.Number.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || o.Customer.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                && (status == "All Statuses" || o.Status == status)).ToList();

            OrderList.ItemsSource = filtered;

            if (EmptyStatePanel != null)
            {
                EmptyStatePanel.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        //------------------------------------------------------------------------------------------------//

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for when an order row is clicked
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

        //------------------------------------------------------------------------------------------------//

        // Adriaan - View order breakdown when clicking VIEW button
        private void BtnViewOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is OrderRow order)
            {
                ShellWindow.Current?.Navigate("breakdown", order);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Adriaan - View order breakdown when clicking on the row
        private void OrderRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is OrderRow order)
            {
                ShellWindow.Current?.Navigate("breakdown", order);
            }
        }

        //------------------------------------------------------------------------------------------------//

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

        //------------------------------------------------------------------------------------------------//

        // Public method to refresh orders, can be called from other parts of the application
        public void RefreshOrders()
        {
            LoadOrdersAsync();
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
