using LogiSyn.Interface;
using SharedLibrary.Model;
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
    /// Interaction logic for UserOrderPageReview.xaml
    /// </summary>
    public partial class UserOrderPageReview : Page
    {
        private readonly OrderScaled _order;
        private readonly IOrderService _orderService;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the UserOrderPageReview class
        public UserOrderPageReview(OrderScaled order)
        {
            InitializeComponent();
            _order = order ?? throw new ArgumentNullException(nameof(order));
            _orderService = new OrderService();

            PopulateUI();
        }

        //------------------------------------------------------------------------------------------------//

        private void PopulateUI()
        {
            TxtOrderTitle.Text = $"{_order.Customer} Order {_order.OrderId}".Trim();
            TxtOrderDate.Text = _order.OrderDate.ToString("d MMMM yyyy");

            // Wrap items with editable presentation bindings
            var presentationItems = _order.productionItems.Select(item => new UserProductionItemViewModel
            {
                ProductName = item.ProductName,
                Amount = "250",
                ProductionLine = item.ProductionLine,
                Packaging = item.packaging,
                Ingredients = item.ReqIngredients.Select(ing => new UserIngredientViewModel
                {
                    IngredientName = ing.IngredientName,
                    DisplayAmount = $"{ing.IngredientAmount} {ing.MeasuredIngredient}",
                    DisplayAdditional = $"{ing.AdditionsAmount} {ing.MeasuredIngredient}",
                    DisplayUsed = $"{ing.AmountUsed} {ing.MeasuredIngredient}"
                }).ToList()
            }).ToList();

            ItemsProductionList.ItemsSource = presentationItems;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Back button click event
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Print button click event
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                MessageBox.Show($"Order sheet for {_order.OrderId} sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Mark Completed button click event
        private void BtnMarkCompleted_Click(object sender, RoutedEventArgs e)
        {
            CompletedModalOverlay.Visibility = Visibility.Visible;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Done button click event in the modal overlay
        private void BtnModalDone_Click(object sender, RoutedEventArgs e)
        {
            // Mark status as completed
            _order.Status = "Completed";

            // Persist update through the shared OrderService
            _orderService.SaveOrder(_order);

            // Return to the user orders dashboard
            NavigationService?.Navigate(new UsersOrderPage());
        }
    }

    //------------------------------------------------------------------------------------------------//

    // View models supporting user data-entry
    public class UserProductionItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public Packaging Packaging { get; set; } = new();
        public List<UserIngredientViewModel> Ingredients { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }

    //------------------------------------------------------------------------------------------------//

    public class UserIngredientViewModel
    {
        public string IngredientName { get; set; } = string.Empty;
        public string DisplayAmount { get; set; } = string.Empty;
        public string DisplayAdditional { get; set; } = string.Empty;
        public string DisplayUsed { get; set; } = string.Empty;
    }
}

//--------------------------------------End of File----------------------------------------------------------//
