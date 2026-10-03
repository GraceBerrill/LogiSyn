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

            // Parse Product Amount
            int.TryParse(TxtProductAmount.Text.Trim(), out int qty);
            if (qty <= 0) qty = 100;

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
            int.TryParse(TxtPackagingAmount.Text.Trim(), out int pkgAmount);
            int.TryParse(TxtPackagingUsed.Text.Trim(), out int pkgUsed);

            // Construct new production line item
            CreatedItem = new ProductionItem
            {
                // Set the product name with quantity
                ProductName = $"{qty} {productTitle}",
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
