using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharedLibrary.Model;
using System.Windows.Input;
using SharedLibrary.Interface;
using AndersonsBakeryAPI.Services;
using System.Diagnostics;
using Microsoft.Win32;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for OrderSheetsView.xaml
    /// </summary>
    public partial class OrderSheetsView : UserControl
    {
        private readonly OrderScaled _currentOrder;
        private readonly IOrderService _orderService;
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly ExcelOrderService _excelService = new ExcelOrderService();

        //------------------------------------------------------------------------------------------------//

        public OrderSheetsView() : this(new OrderScaled())
        {
           
        }

        //------------------------------------------------------------------------------------------------//

        // Constructor for the OrderSheetsView class
        public OrderSheetsView(OrderScaled order)
        {
            InitializeComponent();
            _currentOrder = order ?? throw new ArgumentNullException(nameof(order));
            _orderService = new OrderService();

            PopulateUI();

            // Persist order if it has an OrderId
            if (!string.IsNullOrWhiteSpace(_currentOrder.OrderId))
            {
                _ = SaveOrderAsync();
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Method to populate the UI elements with order details
        private void PopulateUI()
        {
            // Generate order title and date text
            string orderTitle = $"{_currentOrder.Customer} Order {_currentOrder.OrderId}".Trim();
            string orderDateText = _currentOrder.OrderDate.ToString("d MMMM yyyy");

            // Set the text for the order headers and dates on both sides of the UI
            TxtLeftOrderHeader.Text = orderTitle;
            TxtLeftOrderDate.Text = orderDateText;

            TxtRightOrderHeader.Text = orderTitle;
            TxtRightOrderDate.Text = orderDateText;

            // Format production items for the left list
            var displayItems = _currentOrder.productionItems.Select(item => new ProductionItemDisplayModel
            {
                ProductName = item.Amount > 0 && !(item.ProductName ?? string.Empty).StartsWith($"{item.Amount} ")
                    ? $"{item.Amount} {item.ProductName}"
                    : (item.ProductName ?? string.Empty),
                ProductionLine = item.ProductionLine,
                Pans = item.packaging.Pans,
                Trolleys = item.packaging.Trolleys,
                IngredientsSummary = string.Join(",  ", item.ReqIngredients.Select(i =>
                    $"{i.IngredientName}: {i.IngredientAmount + i.AdditionsAmount} {i.MeasuredIngredient}"))
            }).ToList();

            ItemsProductionList.ItemsSource = displayItems;

            // Format raw materials dictionary for the right list
            var displayScaling = _currentOrder.RawMaterials.Select(kvp => new ScalingItemDisplayModel
            {
                Key = kvp.Key,
                DisplayValue = $"{kvp.Value.Amount} {kvp.Value.Unit}"
            }).ToList();

            ItemsScalingList.ItemsSource = displayScaling;
        }

        //------------------------------------------------------------------------------------------------//

        // Helper method to persist order locally and via API
        private async Task SaveOrderAsync()
        {
            try
            {
                _orderService.SaveOrder(_currentOrder);
                try
                {
                    await _apiClient.SaveOrderAsync(_currentOrder);
                }
                catch (Exception apiEx)
                {
                    Console.WriteLine($"API save skipped/failed: {apiEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving order: {ex.Message}");
            }
        }

        private async void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            await SaveOrderAsync();
            NavigateToOrders();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Add Product" button click event
        private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMoreProducts()
            {
                Owner = Window.GetWindow(this)
            };

            // Show the dialog and check if the user created a new production item
            if (dialog.ShowDialog() == true && dialog.CreatedItem != null)
            {
                _currentOrder.productionItems.Add(dialog.CreatedItem);
                _orderService.RecalRawMaterials(_currentOrder);
                PopulateUI();
                await SaveOrderAsync();
            }
        }

        //------------------------------------------------------------------------------------------------//

        private async void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            // Ensure order is persisted when clicking Done
            await SaveOrderAsync();

            // Show the submit modal overlay when the "Done" button is clicked
            SubmitModalOverlay.Visibility = Visibility.Visible;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler to export and save the order as an Excel file
        private async void BtnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            await SaveOrderAsync();

            var dialog = new SaveFileDialog
            {
                Title = "Save Order Production Sheet as Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Order_{_currentOrder.OrderId}_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export the order to an Excel file and get the path
                    string path = _excelService.ExportOrderToExcel(_currentOrder, dialog.FileName);
                    MessageBox.Show($"Order successfully saved as Excel file:\n{path}", "Excel Exported", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigateToOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export Excel file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Print" button click event - exports order as Excel and launches for printing
        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveOrderAsync();

                // Export to Excel file
                string filePath = _excelService.ExportOrderToExcel(_currentOrder);

                // Open Excel file with default system handler so user can print directly from Excel
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);

                MessageBox.Show($"Order {_currentOrder.OrderId} exported as Excel and opened for printing:\n{filePath}", 
                                "Print Excel", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigateToOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Email" button click event - exports order as Excel and attaches to email
        private async void BtnEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveOrderAsync();

                // Export to Excel file
                string filePath = _excelService.ExportOrderToExcel(_currentOrder);

                bool emailSent = false;
                try
                {
                    // Attempt to create and display an Outlook email via late-bound COM
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType)!;
                        dynamic mailItem = outlookApp.CreateItem(0);
                        mailItem.Subject = $"Production Order {_currentOrder.OrderId} - {_currentOrder.Customer}";
                        mailItem.Body = $"Please find attached the production order Excel sheet for {_currentOrder.Customer} (Order {_currentOrder.OrderId}).\n\nPlease enter used quantities and notes in the highlighted columns and upload when complete.";
                        mailItem.Attachments.Add(filePath);
                        mailItem.Display(false);
                        emailSent = true;
                    }
                }
                catch (Exception comEx)
                {
                    Console.WriteLine($"[Outlook COM] Direct Outlook launch unavailable: {comEx.Message}");
                }

                if (!emailSent)
                {
                    // Fallback to default system email client and open folder highlighting the file
                    string subject = Uri.EscapeDataString($"Production Order {_currentOrder.OrderId} - {_currentOrder.Customer}");
                    string body = Uri.EscapeDataString($"Production order Excel file saved at:\n{filePath}\n\nPlease enter used quantities and notes in the highlighted columns.");
                    string mailtoUri = $"mailto:?subject={subject}&body={body}";

                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = mailtoUri, UseShellExecute = true });
                    }
                    catch { }

                    // Highlight the generated Excel file in Windows Explorer for easy dragging/attaching
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{filePath}\"", UseShellExecute = true });
                    MessageBox.Show($"Order exported to Excel:\n{filePath}\n\nPlease attach this file to your email draft.", "Email Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                NavigateToOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Email failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------------------------------------------------------------------------------//


        // Event handler for mouse down event on the submit modal overlay
        private void SubmitModalOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == SubmitModalOverlay)
            {
                SubmitModalOverlay.Visibility = Visibility.Collapsed;
            }
        }

        //------------------------------------------------------------------------------------------------//

        private void NavigateToOrders()
        {
            if (ShellWindow.Current != null)
            {
                ShellWindow.Current.Navigate("orders");
                return;
            }

            var parentWindow = Window.GetWindow(this);
            if (parentWindow?.FindName("MainContent") is ContentControl mainContent)
            {
                mainContent.Content = new AdminOrdersView();
            }
        }

        // Lightweight display models used by OrderSheetsView for UI binding
        public class ProductionItemDisplayModel
        {
            public string ProductName { get; set; } = string.Empty;
            public string ProductionLine { get; set; } = string.Empty;
            public double Pans { get; set; }
            public double Trolleys { get; set; }
            public string IngredientsSummary { get; set; } = string.Empty;
        }

        //------------------------------------------------------------------------------------------------//

        // Lightweight display model for raw materials used by OrderSheetsView for UI binding
        public class ScalingItemDisplayModel
        {
            public string Key { get; set; } = string.Empty;
            public string DisplayValue { get; set; } = string.Empty;
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//