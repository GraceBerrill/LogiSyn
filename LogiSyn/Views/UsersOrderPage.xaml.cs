using AndersonsBakeryAPI.Services;
using AndersonsBakeryAPI.Services;
using SharedLibrary.Interface;
using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ExcelOrderService _excelService = new ExcelOrderService();
        private readonly ApiClient _apiClient = new ApiClient();

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

            // Error message for when no orders are found
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

        // Upload baker Excel spreadsheet with notes and used quantities
        private async void BtnUploadExcel_Click(object sender, RoutedEventArgs e)
        {
            // Triggers the upload file feature filtering by extensions
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Upload Production Excel Sheet",
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Parses the excel data 
                    var parsed = _excelService.ReadOrderFromExcel(dialog.FileName);
                    if (parsed == null)
                    {
                        MessageBox.Show("Could not read valid production order data from the selected Excel file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Orders by Id
                    var existing = _orderService.GetOrderById(parsed.OrderId);
                    OrderScaled orderToUse;
                    if (existing != null)
                    {
                        _excelService.ImportOrderFromExcel(dialog.FileName, existing);
                        orderToUse = existing;
                    }
                    else
                    {
                        orderToUse = parsed;
                    }

                    // Successful import
                    var res = MessageBox.Show(
                        $"Excel sheet for Order {orderToUse.OrderId} imported successfully!\nUsed quantities and notes have been populated.\n\nDo you want to mark this order as Completed now?",
                        "Excel Imported",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        orderToUse.Status = "Completed";
                        _orderService.SaveOrder(orderToUse);
                        try
                        {
                            await _apiClient.SaveOrderAsync(orderToUse);
                            await _apiClient.UpdateOrderStatusAsync(orderToUse.OrderId, "Completed");
                        }
                        catch { }

                        LoadOrders();
                        MessageBox.Show($"Order {orderToUse.OrderId} marked as Completed!", "Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (res == MessageBoxResult.No)
                    {
                        _orderService.SaveOrder(orderToUse);
                        LoadOrders();
                        // Navigate to review so user can view/adjust before completing
                        NavigationService?.Navigate(new UserOrderPageReview(orderToUse));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import Excel: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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