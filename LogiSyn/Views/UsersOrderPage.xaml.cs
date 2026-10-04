using SharedLibrary.Interface;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;

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
    /// Interaction logic for UsersOrderPage.xaml
    /// </summary>
    public partial class UsersOrderPage : Page
    {
        private readonly IOrderService _orderService;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the UsersOrderPage class
        public UsersOrderPage()
        {
            InitializeComponent();
            _orderService = new OrderService();
            TxtCurrentDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
            this.Loaded += (s, e) => LoadOrders();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to load the orders from the order service
        private void LoadOrders()
        {
            var orders = _orderService.GetOrders().ToList();

            // Mock fallback if service currently has no orders
            if (!orders.Any())
            {
                orders = new()
                {
                    new OrderScaled
                    {
                        OrderId = "#001",
                        Customer = "Checkers",
                        OrderDate = DateTime.Now,
                        Status = "Completed"
                    },
                    new OrderScaled
                    {
                        OrderId = "#002",
                        Customer = "Spar",
                        OrderDate = DateTime.Now,
                        Status = "Pending"
                    }
                };
            }

            OrdersItemsControl.ItemsSource = orders;
        }

        //------------------------------------------------------------------------------------------------//

        private void BtnOrderCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderScaled selectedOrder)
            {
                NavigationService?.Navigate(new UserOrderPageReview(selectedOrder));
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//