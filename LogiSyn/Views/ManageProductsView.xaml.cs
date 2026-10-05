using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LogiSyn.Views
{
    public partial class ManageProductsView : UserControl
    {
        private readonly List<ProductRow> _all;
        private readonly ProductService _service = new ProductService();
        private readonly AndersonsBakeryAPI.Services.ApiClient _apiClient = App.ServiceProvider.GetService<AndersonsBakeryAPI.Services.ApiClient>() ?? new AndersonsBakeryAPI.Services.ApiClient();
        private bool _ready;

        public ManageProductsView()
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();
            try
            {
                var products = _apiClient.GetProductsAsync().GetAwaiter().GetResult();
                _all = products.Select(p => new ProductRow
                {
                    ProductId = p.ProductID,
                    Name = p.ProductName,
                    Price = p.PricePerUnit.ToString(),
                    SellBy = p.SellBy.ToString(),
                    BestBefore = p.BestBefore.ToString(),
                    Storage = p.StorageLocation
                }).ToList();
            }
            catch
            {
                _all = _service.GetAll();
            }

            StorageFilter.SelectedIndex = 0;
            _ready = true;
            Refresh();
        }

        /********************************************************************************************/
        //refreshes the product list based on search or filters
        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = StorageFilter.SelectedItem as ComboBoxItem;
            string storage = selected == null ? "Storage" : (string)selected.Content;

            ProductList.ItemsSource = _all.Where(p =>
                (q.Length == 0 || p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                && (storage == "Storage" || p.Storage.Equals(storage, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        private void StorageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private static ProductRow RowOf(object sender)
        {
            return ((FrameworkElement)sender).DataContext as ProductRow;
        }

        /********************************************************************************************/
        //shows the product detail
        private void ShowDetail(ProductRow row, bool editable)
        {
            if (row == null) return;

            Brush overlayScrim = null;
            try
            {
                overlayScrim = FindResource("ScrimDetail") as Brush;
            }
            catch
            {
                overlayScrim = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
            }

            try
            {
                var prod = _service.GetProductByName(row.Name);
                SharedLibrary.Model.ProductDetail detail;
                if (prod != null)
                {
                    var ingredients = new System.Collections.Generic.List<SharedLibrary.Model.IngredientLine>();
                    if (prod.Ingredients != null)
                    {
                        foreach (var ing in prod.Ingredients)
                        {
                            var qtyText = string.Empty;
                            try
                            {
                                if (ing.Quantity > 0) qtyText = ing.Quantity.ToString();
                            }
                            catch { /* ignore if Quantity not numeric or missing */ }
                            if (!string.IsNullOrEmpty(ing.Unit))
                            {
                                qtyText = string.IsNullOrEmpty(qtyText) ? ing.Unit : qtyText + " " + ing.Unit;
                            }

                            ingredients.Add(new SharedLibrary.Model.IngredientLine { Name = ing.IngredientName, Quantity = qtyText });
                        }
                    }

                    detail = new SharedLibrary.Model.ProductDetail
                    {
                        Name = prod.ProductName,
                        DateAdded = "",
                        Ingredients = ingredients,
                        Method = prod.Method,
                        Storage = prod.StorageLocation
                    };
                }
                else
                {
                    detail = SampleData.DetailFor(row);
                }

                var modal = new ProductDetailModal(detail, editable);
                modal.Saved += () =>
                {
                    try
                    {
                        var updated = _service.GetAll();
                        _all.Clear();
                        _all.AddRange(updated);
                        Refresh();
                    }
                    catch { }
                };
                ShellWindow.Current?.ShowModal(modal, overlayScrim);
            }
            catch
            {
                var fallbackModal = new ProductDetailModal(SampleData.DetailFor(row), editable);
                fallbackModal.Saved += () =>
                {
                    try
                    {
                        var updated = _service.GetAll();
                        _all.Clear();
                        _all.AddRange(updated);
                        Refresh();
                    }
                    catch { }
                };
                ShellWindow.Current?.ShowModal(fallbackModal, overlayScrim);
            }
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDetail(RowOf(sender), false);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDetail(RowOf(sender), true);
        }

        /********************************************************************************************/
        //deletes a product from the list after confirmation
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var row = RowOf(sender);
            if (row == null) return;

            var answer = MessageBox.Show("Delete " + row.Name + "?", "Delete product",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            // Try API delete by id first, fallback to local delete by name
            try
            {
                var deleted = false;
                try
                {
                    if (row.ProductId > 0)
                    {
                        deleted = _apiClient.DeleteProductAsync(row.ProductId.ToString()).GetAwaiter().GetResult();
                    }
                }
                catch { }

                if (!deleted)
                {
                    try { _service.DeleteByName(row.Name); }
                    catch { }
                }

                _all.Remove(row);
                Refresh();
            }
            catch { }
        }

        /********************************************************************************************/
        //adds a new product to the list
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            Brush overlayScrim = null;
            try
            {
                overlayScrim = FindResource("ScrimDetail") as Brush;
            }
            catch
            {
                overlayScrim = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
            }

            var modal = new AddProductModal();
            modal.Saved += () =>
            {
                try
                {
                    var updated = _service.GetAll();
                    _all.Clear();
                    _all.AddRange(updated);
                    Refresh();
                }
                catch { }
            };
            ShellWindow.Current?.ShowModal(modal, overlayScrim);
        }
    }
}
/*********************************************MAR26EOF*******************************************/