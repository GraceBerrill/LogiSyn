using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class AddProductModal : UserControl
    {
        public AddProductModal()
        {
            InitializeComponent();
            Loaded += (s, e) => ProductBox.Focus();
        }

        /********************************************************************************************/
        //add a product to the list and close the model
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var name = ProductBox.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show("Please enter the product name.", "Add Product",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string method = string.Empty;
            var methodBox = this.FindName("MethodBox") as TextBox;
            if (methodBox != null) method = methodBox.Text.Trim();
            if (string.IsNullOrEmpty(method))
            {
                MessageBox.Show("Please enter the preparation method.", "Add Product",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse((ProductPriceBox?.Text ?? string.Empty).Replace("R", "").Trim(), out var price) || price <= 0)
            {
                MessageBox.Show("Please enter a valid price greater than 0.", "Add Product",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(SellByBox?.Text.Trim() ?? string.Empty, out var sellBy) || sellBy <= 0)
            {
                MessageBox.Show("Please enter a valid Sell By value greater than 0.", "Add Product",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(BestBeforeBox?.Text.Trim() ?? string.Empty, out var bestBefore) || bestBefore <= 0)
            {
                MessageBox.Show("Please enter a valid Best Before value greater than 0.", "Add Product",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var storage = string.Empty;
            try { storage = (StorageBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty; } catch { }

            var prod = new LogiSyn.Model.Product
            {
                ProductName = name,
                PricePerUnit = price,
                SellBy = sellBy,
                BestBefore = bestBefore,
                StorageLocation = storage,
                Method = method,
                Ingredients = new System.Collections.Generic.List<LogiSyn.Model.IngredientRequirement>()
            };

            for (int i = 1; i <= 3; i++)
            {
                var inName = this.FindName($"Ing{i}Name") as TextBox;
                var inQty = this.FindName($"Ing{i}Qty") as TextBox;
                var inUnit = this.FindName($"Ing{i}Unit") as TextBox;
                if (inName == null) break;
                var iname = inName.Text.Trim();
                if (string.IsNullOrEmpty(iname)) continue;
                decimal qty = 0;
                if (inQty != null) decimal.TryParse(inQty.Text.Trim(), out qty);
                var unit = inUnit?.Text.Trim() ?? string.Empty;
                prod.Ingredients.Add(new LogiSyn.Model.IngredientRequirement { IngredientName = iname, Quantity = qty, Unit = string.IsNullOrEmpty(unit) ? "units" : unit });
            }

            var svc = new LogiSyn.Services.ProductService();
            try
            {
                svc.Add(prod);
            }
            catch { }

            ShellWindow.Current.CloseModal();
        }
    }
}
/*********************************************MAR26EOF*******************************************/