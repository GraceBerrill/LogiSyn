// Adriaan
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
<<<<<<< HEAD
using SharedLibrary.Model;
=======
using LogiSyn.Model;
using LogiSyn.Services;
>>>>>>> Adriaan

namespace LogiSyn.Views
{
    public partial class HistoryView : UserControl
    {
        private List<OrderRow> _all = new();
        private bool _ready;
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the HistoryView class
        public HistoryView(AppRole role)
        {
            InitializeComponent();

            // Set the date text and header based on the role
            DateText.Text = SampleData.Today();
            DateHeader.Text = role == AppRole.Admin ? "Date Completed" : "Date";

            DateFilter.SelectedIndex = 0;
            _ready = true;

            LoadHistoryAsync();
        }

        //------------------------------------------------------------------------------------------------//

        // Load the order history asynchronously from the API or local OrderService
        private async void LoadHistoryAsync()
        {
            try
            {
                // Fetch orders from the API
                var orders = await _apiClient.GetOrdersAsync();
                if (orders.Count == 0)
                {
                    orders = _orderService.GetOrders().ToList();
                }

                var completed = orders
                    .Where(o => string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase))
                    .Select(o => OrderRow.FromOrderScaled(o))
                    .ToList();

                _all = completed.Count > 0 ? completed : SampleData.HistoryOrders();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading history: {ex.Message}");
                var localOrders = _orderService.GetHistory()
                    .Select(o => OrderRow.FromOrderScaled(o))
                    .ToList();
                _all = localOrders.Count > 0 ? localOrders : SampleData.HistoryOrders();
            }

            Refresh();
        }

        //------------------------------------------------------------------------------------------------//

        // Refresh the order list based on the search query and selected date filter
        private void Refresh()
        {
            // Check if the view is ready before proceeding
            if (!_ready) return;

            // Get the search query and selected date filter
            string q = SearchBox.Text.Trim();
            var selected = DateFilter.SelectedItem as ComboBoxItem;
            string sort = selected == null ? "DATE" : (string)selected.Content;

            // Searches the order list based on the search query
            IEnumerable<OrderRow> rows = _all.Where(o =>
                q.Length == 0
                || o.Number.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || o.Customer.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

            // Sort the order list based on the selected date filter
            if (sort == "Newest first") rows = rows.OrderByDescending(o => o.Date);
            else if (sort == "Oldest first") rows = rows.OrderBy(o => o.Date);

            OrderList.ItemsSource = rows.ToList();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the SearchBox text changed event
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the DateFilter selection changed event
        private void DateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the ViewButton click event to navigate to the breakdown view for the selected order
        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            var order = ((FrameworkElement)sender).DataContext as OrderRow;
            ShellWindow.Current?.Navigate("breakdown", order);
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//