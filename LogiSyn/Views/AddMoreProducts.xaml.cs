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
    public partial class AddMoreProducts : Window
    {
        public ProductionItem? CreatedItem { get; private set; }

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

        /********************************************************************************************/
        //buttons handdler for add button
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string productTitle = TxtProduct.Text.Trim();
            if (string.IsNullOrWhiteSpace(productTitle))
            {
                MessageBox.Show("Please enter a product name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(TxtProductAmount.Text.Trim(), out int qty);
            if (qty <= 0) qty = 100;

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

            //parses the ingredients
            double.TryParse(TxtIngredientAmount.Text.Trim(), out double ingAmount);
            double.TryParse(TxtIngredientAdditional.Text.Trim(), out double ingAdditional);
            double.TryParse(TxtIngredientUsed.Text.Trim(), out double ingUsed);
            string ingName = string.IsNullOrWhiteSpace(TxtIngredientName.Text)
                ? "Flour"
                : TxtIngredientName.Text.Trim();

            double.TryParse(TxtPackagingAmount.Text.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pkgAmount);
            double.TryParse(TxtPackagingUsed.Text.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pkgUsed);

            //creates new product
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

                //set ingredients
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


            DialogResult = true;
            Close();
        }
    }
}
/*********************************************MAR26EOF*******************************************/
