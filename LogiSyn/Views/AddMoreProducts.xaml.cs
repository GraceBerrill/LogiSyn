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

            // Parses the ingredients
            double.TryParse(TxtIngredientAmount.Text.Trim(), out double ingAmount);
            double.TryParse(TxtIngredientAdditional.Text.Trim(), out double ingAdditional);
            double.TryParse(TxtIngredientUsed.Text.Trim(), out double ingUsed);
            string userIngName = TxtIngredientName.Text.Trim();
            string userUnit = !string.IsNullOrWhiteSpace(TxtIngredientUnit.Text)
                ? TxtIngredientUnit.Text.Trim()
                : "kg";

            double.TryParse(TxtPackagingAmount.Text.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pkgAmount);
            double.TryParse(TxtPackagingUsed.Text.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pkgUsed);

            var reqIngredients = new List<Ingredients>();

            if (!string.IsNullOrWhiteSpace(userIngName))
            {
                // User explicitly provided an ingredient name
                reqIngredients.Add(new Ingredients
                {
                    IngredientName = userIngName,
                    IngredientAmount = ingAmount,
                    AdditionsAmount = ingAdditional,
                    MeasuredIngredient = userUnit,
                    AmountUsed = ingUsed
                });
            }
            else
            {
                // Check if product exists in recipe service or product database
                var recipeService = new AndersonsBakeryAPI.Services.TempRecipeService();
                var recipe = recipeService.FindRecipeByProductName(productTitle);

                if (recipe != null && recipe.Ingredients != null && recipe.Ingredients.Count > 0)
                {
                    foreach (var rIng in recipe.Ingredients)
                    {
                        reqIngredients.Add(new Ingredients
                        {
                            IngredientName = rIng.Name,
                            IngredientAmount = Math.Round(rIng.AmountPerUnit * qty, 3),
                            AdditionsAmount = Math.Round(rIng.AdditionalRatio * qty, 3),
                            MeasuredIngredient = rIng.Unit ?? userUnit,
                            AmountUsed = 0
                        });
                    }

                    if (pkgAmount <= 0 && recipe.UnitsPerPan > 0)
                    {
                        pkgAmount = Math.Ceiling((double)qty / recipe.UnitsPerPan);
                    }
                }
                else
                {
                    var productService = new AndersonsBakeryAPI.Services.ProductService();
                    var prod = productService.GetProductByName(productTitle);
                    if (prod != null && prod.Ingredients != null && prod.Ingredients.Count > 0)
                    {
                        foreach (var pIng in prod.Ingredients)
                        {
                            double baseAmt = (double)pIng.Quantity;
                            reqIngredients.Add(new Ingredients
                            {
                                IngredientName = pIng.IngredientName,
                                IngredientAmount = Math.Round(baseAmt * qty, 3),
                                AdditionsAmount = Math.Round(baseAmt * 0.1 * qty, 3),
                                MeasuredIngredient = pIng.Unit ?? userUnit,
                                AmountUsed = 0
                            });
                        }
                    }
                }

                if (reqIngredients.Count == 0)
                {
                    MessageBox.Show("Please enter an ingredient name and unit for this product.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtIngredientName.Focus();
                    return;
                }
            }

            double finalPans = pkgAmount > 0 ? pkgAmount : 2;
            int finalTrolleys = (int)Math.Ceiling(finalPans / 2.0);

            // Creates new product
            CreatedItem = new ProductionItem
            {
                ProductName = productTitle,
                Amount = qty,
                ProductionLine = productionLine,
                packaging = new Packaging
                {
                    Pans = finalPans,
                    Trolleys = finalTrolleys > 0 ? finalTrolleys : 1,
                    PansUsed = pkgUsed,
                    TrolleysUsed = 0
                },
                ReqIngredients = reqIngredients
            };

            DialogResult = true;
            Close();
        }
    }
}
/*********************************************MAR26EOF*******************************************/
