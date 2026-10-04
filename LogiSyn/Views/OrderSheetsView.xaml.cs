// Adriaan
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
using LogiSyn.Model;
using LogiSyn.Interface;
using LogiSyn.Services;
using System.Diagnostics;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for OrderReviewPage.xaml
    /// </summary>
    public partial class OrderSheetsView : UserControl
    {
        private readonly OrderScaled _currentOrder;
        private readonly IOrderService _orderService;
        private readonly ApiClient _apiClient = new ApiClient();

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
                ProductName = item.ProductName,
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

        // Event handler for the "Print" button click event
        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            // Save the current order and attempt to print it
            try
            {
                await SaveOrderAsync();

                // Show the print dialog and check if the user confirmed printing
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    MessageBox.Show($"Order {_currentOrder.OrderId} sent to printer.", "Printing", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Navigate back to the OrdersPage after printing
                NavigateToOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Email" button click event
        private async void BtnEmail_Click(object sender, RoutedEventArgs e)
        {
            // Save the current order and attempt to email it
            try
            {
                await SaveOrderAsync();

                var emailModel = _orderService.BuildScalingSheetEmail(_currentOrder);

                string recipient = "";
                string subject = Uri.EscapeDataString(emailModel.Subject);
                string body = Uri.EscapeDataString(emailModel.Body);
                string mailtoUri = $"mailto:{recipient}?subject={subject}&body={body}";

                var psi = new ProcessStartInfo
                {
                    FileName = mailtoUri,
                    UseShellExecute = true
                };

                Process.Start(psi);
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

        // Lightweight display models used by OrderReviewPage for UI binding
        internal class ProductionItemDisplayModel
        {
            public string ProductName { get; set; } = string.Empty;
            public string ProductionLine { get; set; } = string.Empty;
            public double Pans { get; set; }
            public double Trolleys { get; set; }
            public string IngredientsSummary { get; set; } = string.Empty;
        }

        //------------------------------------------------------------------------------------------------//

        // Lightweight display model for raw materials used by OrderReviewPage for UI binding
        internal class ScalingItemDisplayModel
        {
            public string Key { get; set; } = string.Empty;
            public string DisplayValue { get; set; } = string.Empty;
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//