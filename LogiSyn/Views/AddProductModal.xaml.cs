using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LogiSyn.Views
{
    public partial class AddProductModal : UserControl
    {
        public ObservableCollection<IngredientInputRow> IngredientsList { get; set; } = new();
        public event Action? Saved;

        public AddProductModal()
        {
            InitializeComponent();

            IngredientsList.Add(new IngredientInputRow());
            IngredientsList.Add(new IngredientInputRow());
            IngredientsList.Add(new IngredientInputRow());

            IngredientsItemsControl.ItemsSource = IngredientsList;

            Loaded += (s, e) => ProductBox.Focus();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current?.CloseModal();
        }

        private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            IngredientsList.Add(new IngredientInputRow());
        }

        /********************************************************************************************/
        //remove ingredient row from the list
        private void RemoveIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is IngredientInputRow row)
            {
                if (IngredientsList.Count > 1)
                {
                    IngredientsList.Remove(row);
                }
                else
                {
                    row.Name = string.Empty;
                    row.Quantity = string.Empty;
                    row.Unit = string.Empty;
                }
            }
        }

        /********************************************************************************************/
        // Add product to the list of products and save it to the file
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = ProductBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ShowValidationError("Please enter the product name.", ProductBox);
                return;
            }

            string method = MethodBox.Text.Trim();
            if (string.IsNullOrEmpty(method))
            {
                ShowValidationError("Please enter the preparation method.", MethodBox);
                return;
            }

            if (!TryParseDecimal(ProductPriceBox.Text, out var price) || price <= 0)
            {
                ShowValidationError("Please enter a valid price greater than 0.", ProductPriceBox);
                return;
            }

            if (!int.TryParse(SellByBox.Text.Trim(), out var sellBy) || sellBy <= 0)
            {
                ShowValidationError("Please enter a valid Sell By value (days) greater than 0.", SellByBox);
                return;
            }

            if (!int.TryParse(BestBeforeBox.Text.Trim(), out var bestBefore) || bestBefore <= 0)
            {
                ShowValidationError("Please enter a valid Best Before value (days) greater than 0.", BestBeforeBox);
                return;
            }

            var storage = (StorageBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

            var product = new Product
            {
                ProductName = name,
                PricePerUnit = price,
                SellBy = sellBy,
                BestBefore = bestBefore,
                StorageLocation = storage,
                Method = method,
                Ingredients = GetIngredientList()
            };

            try
            {
                var api = App.ServiceProvider.GetService<ApiClient>() ?? new ApiClient();
                bool created = await api.CreateProductAsync(product);
                if (created)
                {
                    Saved?.Invoke();
                    ShellWindow.Current?.CloseModal();
                    return;
                }

                // Fallback to local ProductService if API unavailable
                var service = App.ServiceProvider.GetService<ProductService>() ?? new ProductService();
                service.Add(product);
                Saved?.Invoke();
                ShellWindow.Current?.CloseModal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving product: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /********************************************************************************************/
        // List of ingredients for the product (parses dynamic list)
        private List<IngredientRequirement> GetIngredientList()
        {
            var ingredients = new List<IngredientRequirement>();

            foreach (var row in IngredientsList)
            {
                string name = row.Name?.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                TryParseDecimal(row.Quantity, out decimal qty);
                string unit = row.Unit?.Trim();

                ingredients.Add(new IngredientRequirement
                {
                    IngredientName = name,
                    Quantity = qty,
                    Unit = string.IsNullOrEmpty(unit) ? "units" : unit
                });
            }

            return ingredients;
        }

        private static void ShowValidationError(string message, Control controlToFocus)
        {
            MessageBox.Show(message, "Add Product", MessageBoxButton.OK, MessageBoxImage.Information);
            controlToFocus?.Focus();
        }

        /********************************************************************************************/
        // Makes decimals parseable
        private static bool TryParseDecimal(string input, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string s = input.Trim().Replace("R", "").Replace("$", "").Replace("€", "").Replace("£", "").Trim();

            if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
                return true;

            if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out value))
                return true;

            s = s.Replace(",", "");
            return decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }
    }

    /********************************************************************************************/
    public class IngredientInputRow
    {
        public string Name { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }
}
/*********************************************MAR26EOF*******************************************/