// Adriaan
using LogiSyn.Interface;
using LogiSyn.Model;
using LogiSyn.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for AdminOrderHistory.xaml
    /// </summary>
    public partial class AdminOrderHistory : Page
    {
        private readonly IOrderService _orderService;
        private List<OrderScaled> _completedOrders = new();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the AdminOrderHistory class
        public AdminOrderHistory()
        {
            InitializeComponent();

            _orderService = new OrderService();
            TxtCurrentDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
            this.Loaded += (s, e) => LoadHistory();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to load the order history from the order service
        private void LoadHistory()
        {
            var history = _orderService.GetHistory().ToList();

            // If there are no orders in the history, create mock data for testing
            if (!history.Any())
            {
                // Display text indicating no orders are available
                TxtNoOrders.Visibility = Visibility.Visible;

                history = _orderService.GetHistory().ToList();
            }
            else
            {
                TxtNoOrders.Visibility = Visibility.Collapsed;
            }

            _completedOrders = history;
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to apply filters based on search text and date sort selection
        private void ApplyFilters()
        {
            // Defensive guard: HistoryDataGrid may be null during InitializeComponent if events fire early
            if (HistoryDataGrid == null)
                return;

            // Start with the full list of completed orders
            var filtered = _completedOrders.AsEnumerable();

            // Apply search filter based on the text in the search box
            string search = TxtSearchBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(o =>
                    (!string.IsNullOrEmpty(o.OrderId) && o.OrderId.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(o.Customer) && o.Customer.ToLower().Contains(search)));
            }

            // Apply date sort filter based on the selected item in the date sort combo box
            if (CmbDateSort?.SelectedItem is ComboBoxItem selectedItem)
            {
                string sort = selectedItem.Content?.ToString() ?? "";
                if (sort == "Newest First")
                    filtered = filtered.OrderByDescending(o => o.OrderDate);
                else if (sort == "Oldest First")
                    filtered = filtered.OrderBy(o => o.OrderDate);
            }

            var list = filtered.ToList();
            HistoryDataGrid.ItemsSource = null;
            HistoryDataGrid.ItemsSource = list;

            if (TxtNoOrders != null)
            {
                TxtNoOrders.Visibility = (!_completedOrders.Any() || !list.Any())
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }


        //------------------------------------------------------------------------------------------------//

        // Event handler for when the text in the search box changes
        private void TxtSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for when the selection in the date sort combo box changes
        private void CmbDateSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "View" button click event in the order history data grid
        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderScaled selectedOrder)
            {
                NavigationService?.Navigate(new AdminOrderBreakdown(selectedOrder));
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
