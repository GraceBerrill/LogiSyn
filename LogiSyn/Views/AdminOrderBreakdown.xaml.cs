using SharedLibrary.Model;
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
    /// Interaction logic for AdminOrderBreakdown.xaml
    /// </summary>
    public partial class AdminOrderBreakdown : Page
    {
        private readonly OrderScaled _order;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the AdminOrderBreakdown class
        public AdminOrderBreakdown(OrderScaled order)
        {
            InitializeComponent();

            _order = order ?? throw new ArgumentNullException(nameof(order));

            PopulateUI();
        }

        //------------------------------------------------------------------------------------------------//

        private void PopulateUI()
        {
            TxtOrderTitle.Text = $"{_order.Customer} Order {_order.OrderId}".Trim();
            TxtOrderDate.Text = _order.OrderDate.ToString("d MMMM yyyy");
            TxtStatus.Text = string.IsNullOrWhiteSpace(_order.Status) ? "COMPLETED" : _order.Status.ToUpper();

            // Prepare the list of items for display in the breakdown
            var itemsList = (_order.productionItems ?? _order.productionItems ?? new List<ProductionItem>())
                .Select(item =>
                {
                    // Extract the product name and amount from the ProductName property
                    string rawName = item.ProductName ?? string.Empty;
                    string amount = string.Empty;
                    string name = rawName;

                    var parts = rawName.Split(' ', 2);
                    if (parts.Length == 2 && int.TryParse(parts[0], out _))
                    {
                        amount = parts[0];
                        name = parts[1];
                    }
                    else
                    {
                        amount = "100";
                    }

                    var pkg = item.packaging ?? item.packaging ?? new Packaging();

                    // Create a new BreakdownProductItemViewModel for each production item
                    return new BreakdownProductItemViewModel
                    {
                        DisplayProductName = name,
                        DisplayAmount = amount,
                        ProductionLine = item.ProductionLine ?? "Production 1",
                        PansAmount = pkg.Pans.ToString(),
                        PansUsed = (pkg.PansUsed > 0 ? pkg.PansUsed : pkg.Pans).ToString(),
                        TrolleysAmount = pkg.Trolleys.ToString(),
                        TrolleysUsed = (pkg.TrolleysUsed > 0 ? pkg.TrolleysUsed : pkg.Trolleys).ToString(),
                        Notes = string.IsNullOrWhiteSpace(item.Notes) ? "No additional notes recorded." : item.Notes,
                        Ingredients = (item.ReqIngredients ?? new List<Ingredients>()).Select(ing => new BreakdownIngredientItemViewModel
                        {
                            IngredientName = ing.IngredientName,
                            Amount = $"{ing.IngredientAmount} {ing.MeasuredIngredient}",
                            Additional = $"{ing.AdditionsAmount} {ing.MeasuredIngredient}",
                            Used = $"{(ing.AmountUsed > 0 ? ing.AmountUsed : ing.IngredientAmount)} {ing.MeasuredIngredient}"
                        }).ToList()
                    };
                }).ToList();

            ItemsBreakdownList.ItemsSource = itemsList;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Done" button click event
        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AdminOrderHistory());
        }
    }

    //------------------------------------------------------------------------------------------------//

    // ViewModel for displaying product breakdown information
    public class BreakdownProductItemViewModel
    {
        public string DisplayProductName { get; set; } = string.Empty;
        public string DisplayAmount { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public string PansAmount { get; set; } = string.Empty;
        public string PansUsed { get; set; } = string.Empty;
        public string TrolleysAmount { get; set; } = string.Empty;
        public string TrolleysUsed { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<BreakdownIngredientItemViewModel> Ingredients { get; set; } = new();
    }

    //------------------------------------------------------------------------------------------------//

    // ViewModel for displaying ingredient breakdown information
    public class BreakdownIngredientItemViewModel
    {
        public string IngredientName { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Additional { get; set; } = string.Empty;
        public string Used { get; set; } = string.Empty;
    }
}

//--------------------------------------End of File----------------------------------------------------------//
