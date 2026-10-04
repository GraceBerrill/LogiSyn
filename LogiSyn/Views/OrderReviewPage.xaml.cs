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
using SharedLibrary.Model;
using SharedLibrary.Interface;
using AndersonsBakeryAPI.Services;


namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for OrderReviewPage.xaml
    /// </summary>
    public partial class OrderReviewPage : Page
    {
        private readonly OrderScaled _currentOrder;
        private readonly IOrderService _orderService;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the OrderReviewPage class
        public OrderReviewPage(OrderScaled order)
        {
            InitializeComponent();
            _currentOrder = order ?? throw new ArgumentNullException(nameof(order));
            _orderService = new OrderService();

            PopulateUI();
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

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Add Product" button click event
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
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
            }
        }

        //------------------------------------------------------------------------------------------------//

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            // Show the submit modal overlay when the "Done" button is clicked
            SubmitModalOverlay.Visibility = Visibility.Visible;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Print" button click event
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            // Save the current order and attempt to print it
            try
            {
                _orderService.SaveOrder(_currentOrder);

                // Show the print dialog and check if the user confirmed printing
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    MessageBox.Show($"Order {_currentOrder.OrderId} sent to printer.", "Printing", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Navigate back to the OrdersPage after printing
                NavigationService?.Navigate(new OrdersPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Email" button click event
        private void BtnEmail_Click(object sender, RoutedEventArgs e)
        {
            // Save the current order and attempt to email it
            try
            {
                _orderService.SaveOrder(_currentOrder);

                MessageBox.Show($"Scaling sheet for Order {_currentOrder.OrderId} emailed to dispatch.", "Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService?.Navigate(new OrdersPage());
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