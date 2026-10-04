// Adriaan
using System;
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
    public partial class UserOrdersView : UserControl
    {
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();
        private readonly ExcelOrderService _excelService = new ExcelOrderService();

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

        // Adriaan - Upload baker Excel spreadsheet with notes and used quantities
        private async void BtnUploadExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Upload Production Excel Sheet",
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var parsed = _excelService.ReadOrderFromExcel(dialog.FileName);
                    if (parsed == null)
                    {
                        MessageBox.Show("Could not read valid production order data from the selected Excel file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

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

                        LoadOrdersAsync();
                        MessageBox.Show($"Order {orderToUse.OrderId} marked as Completed!", "Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (res == MessageBoxResult.No)
                    {
                        _orderService.SaveOrder(orderToUse);
                        LoadOrdersAsync();
                        ShellWindow.Current?.Navigate("usersheet", OrderRow.FromOrderScaled(orderToUse));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import Excel: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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