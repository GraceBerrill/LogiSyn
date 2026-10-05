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
using System.Windows.Shapes;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for AddMoreProducts.xaml
    /// </summary>
    public partial class AddMoreProducts : Window
    {
        public ProductionItem? CreatedItem { get; private set; }

        //------------------------------------------------------------------------------------------------//

        // Constructor for the AddMoreProducts window
        public AddMoreProducts()
        {
            InitializeComponent();

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        //------------------------------------------------------------------------------------------------//

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string productTitle = TxtProduct.Text.Trim();
            if (string.IsNullOrWhiteSpace(productTitle))
            {
                MessageBox.Show("Please enter a product name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate and parse Product Amount - must be a valid positive integer
            string rawAmount = TxtProductAmount.Text.Trim();
            if (string.IsNullOrWhiteSpace(rawAmount) || !int.TryParse(rawAmount, out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid positive integer for the product amount (e.g. 50, 100).",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtProductAmount.Focus();
                TxtProductAmount.SelectAll();
                return;
            }

            // If quantity was embedded in the product name (e.g. "50 Hamburger Rolls"), strip it out and set Amount explicitly
            var qtyMatch = System.Text.RegularExpressions.Regex.Match(productTitle, @"^(\d+)\s+(.+)$");
            if (qtyMatch.Success)
            {
                if (int.TryParse(qtyMatch.Groups[1].Value, out int extractedQty))
                {
                    qty = extractedQty;
                }
                productTitle = qtyMatch.Groups[2].Value.Trim();
            }

            string productionLine = string.IsNullOrWhiteSpace(TxtProduction.Text)
                ? "Production 1"
                : TxtProduction.Text.Trim();

            // Parse Ingredients
            double.TryParse(TxtIngredientAmount.Text.Trim(), out double ingAmount);
            double.TryParse(TxtIngredientAdditional.Text.Trim(), out double ingAdditional);
            double.TryParse(TxtIngredientUsed.Text.Trim(), out double ingUsed);
            string ingName = string.IsNullOrWhiteSpace(TxtIngredientName.Text)
                ? "Flour"
                : TxtIngredientName.Text.Trim();

            // Parse Packaging
            double.TryParse(TxtPackagingAmount.Text.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pkgAmount);
            double.TryParse(TxtPackagingUsed.Text.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pkgUsed);

            // Construct new production line item
            CreatedItem = new ProductionItem
            {
                ProductName = productTitle,
                Amount = qty,
                ProductionLine = productionLine,
                packaging = new Packaging
                {
                    Pans = pkgAmount > 0 ? pkgAmount : 2,
                    Trolleys = 1,
                    PansUsed = pkgUsed,
                    TrolleysUsed = 0
                },
                // Set the required ingredients
                ReqIngredients = new List<Ingredients>
                {
                    new Ingredients
                    {
                        IngredientName = ingName,
                        IngredientAmount = ingAmount,
                        AdditionsAmount = ingAdditional,
                        MeasuredIngredient = "bags",
                        AmountUsed = ingUsed
                    }
                }
            };

            // Close the dialog and return the created item

            DialogResult = true;
            Close();
        }
    }
}

//------------------------------------------------------------------------------------------------//
