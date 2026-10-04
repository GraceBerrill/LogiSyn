// Adriaan
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class UserOrdersView : UserControl
    {
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the UserOrdersView class
        public UserOrdersView()
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();

            LoadOrdersAsync();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to load user orders asynchronously
        private async void LoadOrdersAsync()
        {
            // Load admin orders sent to users from the API or local OrderService
            try
            {
                var orders = await _apiClient.GetOrdersAsync();
                if (orders.Count == 0)
                {
                    orders = _orderService.GetOrders().ToList();
                }

                var rows = orders.Select(o => OrderRow.FromOrderScaled(o)).ToList();
                OrderList.ItemsSource = rows.Count > 0 ? rows : SampleData.UserOrders();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user orders: {ex.Message}");
                var localOrders = _orderService.GetOrders().ToList();
                var rows = localOrders.Select(o => OrderRow.FromOrderScaled(o)).ToList();
                OrderList.ItemsSource = rows.Count > 0 ? rows : SampleData.UserOrders();
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for when an order row is clicked
        private void OrderRow_Click(object sender, RoutedEventArgs e)
        {
            var order = ((FrameworkElement)sender).DataContext as OrderRow;
            ShellWindow.Current?.Navigate("usersheet", order);
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//