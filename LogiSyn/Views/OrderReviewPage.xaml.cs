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

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for OrderReviewPage.xaml
    /// </summary>
    public partial class OrderReviewPage : Page
    {
        private readonly OrderScaled _currentOrder;
        private readonly IOrderService _orderService;

        public OrderReviewPage(OrderScaled order)
        {
            InitializeComponent();
            _currentOrder = order ?? throw new ArgumentNullException(nameof(order));
            _orderService = new OrderService();

            PopulateUI();
        }

        private void PopulateUI()
        {
            string orderTitle = $"{_currentOrder.Customer} Order {_currentOrder.OrderId}".Trim();
            string orderDateText = _currentOrder.OrderDate.ToString("d MMMM yyyy");

            // Left Card Headers
            TxtLeftOrderHeader.Text = orderTitle;
            TxtLeftOrderDate.Text = orderDateText;

            // Right Card Headers
            TxtRightOrderHeader.Text = orderTitle;
            TxtRightOrderDate.Text = orderDateText;

            // Format items for the left list with concatenated ingredient strings
            var displayItems = _currentOrder.productionItems.Select(item => new ProductionItemDisplayModel
            {
                ProductName = item.ProductName,
                ProductionLine = item.ProductionLine,
                Pans = item.packaging.Pans,
                // Safely reads Trollies from your Packaging model
                Trollies = item.packaging.Trollies,
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

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Manual product entry dialog will be linked here.", "Add Product", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save finalized order into memory/store
                _orderService.SaveOrder(_currentOrder);

                MessageBox.Show($"Order {_currentOrder.OrderId} confirmed and sent to production.", "Order Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                // Navigate back to the Orders history table
                NavigationService?.Navigate(new OrdersPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    // Lightweight display models used by OrderReviewPage for UI binding
    internal class ProductionItemDisplayModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public int Pans { get; set; }
        public int Trollies { get; set; }
        public string IngredientsSummary { get; set; } = string.Empty;
    }

    internal class ScalingItemDisplayModel
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
    }
    }
}