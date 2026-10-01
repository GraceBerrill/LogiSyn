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
    /// Interaction logic for OrdersPage.xaml
    /// </summary>
    public partial class OrdersPage : Page
    {
        // Initialise the order service and a list to hold all orders
        private readonly IOrderService _orderService;
        private List<OrderScaled> _allOrders = new();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the OrdersPage class
        public OrdersPage()
        {
            InitializeComponent();
            _orderService = new OrderService();
            TxtCurrentDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
            this.Loaded += OrdersPage_Loaded;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Loaded event of the OrdersPage
        private void OrdersPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to load orders from the order service
        private void LoadOrders()
        {
            //TODO: Remove this block when the database is populated with real orders
            if (!_orderService.GetOrders().Any())
            {
                _orderService.SaveOrder(new OrderScaled
                {
                    OrderId = "#001",
                    Customer = "Checkers",
                    OrderDate = DateTime.Now,
                    Status = "Pending"
                });
                _orderService.SaveOrder(new OrderScaled
                {
                    OrderId = "#002",
                    Customer = "Spar",
                    OrderDate = DateTime.Now,
                    Status = "Complete"
                });
            }

            _allOrders = _orderService.GetOrders().ToList();
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to apply filters based on search text and selected status
        private void ApplyFilters()
        {
            var filtered = _allOrders.AsEnumerable();

            // Apply search filter
            string search = TxtSearchBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search) && search != "search orders...")
            {
                filtered = filtered.Where(o =>
                    (!string.IsNullOrEmpty(o.OrderId) && o.OrderId.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(o.Customer) && o.Customer.ToLower().Contains(search)));
            }

            // Apply status filter
            if (CmbStatusFilter?.SelectedItem is ComboBoxItem selectedItem)
            {
                string status = selectedItem.Content?.ToString() ?? "";
                if (!string.IsNullOrEmpty(status) && status != "All Statuses")
                {
                    filtered = filtered.Where(o =>
                        !string.IsNullOrEmpty(o.Status) &&
                        o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Update the DataGrid with the filtered list
            if (OrdersDataGrid != null)
            {
                OrdersDataGrid.ItemsSource = null;
                OrdersDataGrid.ItemsSource = filtered.ToList();
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the TextChanged event of the search box
        private void TxtSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the SelectionChanged event of the status filter ComboBox
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Click event of the "Create Order" button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CreateOrderPage());
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
