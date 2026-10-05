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
        private List<ProductRow> _all = new();
        private readonly ProductService _service;
        private readonly AndersonsBakeryAPI.Services.ApiClient _apiClient = App.ServiceProvider.GetService<AndersonsBakeryAPI.Services.ApiClient>() ?? new AndersonsBakeryAPI.Services.ApiClient();
        private bool _ready;

        public ManageProductsView()
        {
            InitializeComponent();

            DateText.Text = DateTime.Now.ToString("yyyy-MM-dd");
            _service = App.ServiceProvider.GetService<ProductService>() ?? new ProductService();

            Loaded += async (s, e) => await LoadProductsAsync();
        }

        private async System.Threading.Tasks.Task LoadProductsAsync()
        {
            try
            {
                var products = await _apiClient.GetProductsAsync();
                if (products != null && products.Count > 0)
                {
                    _all = products.Select(p => new ProductRow
                    {
                        Id = p.Id,
                        ProductId = p.ProductID,
                        Name = p.ProductName,
                        Price = p.PricePerUnit > 0 ? ("R" + p.PricePerUnit.ToString("0.00")) : string.Empty,
                        SellBy = p.SellBy.ToString(),
                        BestBefore = p.BestBefore.ToString(),
                        Storage = p.StorageLocation
                    }).ToList();
                }
                else
                {
                    _all = _service.GetAll();
                }
            }
            catch
            {
                try { _all = _service.GetAll(); }
                catch { _all = new List<ProductRow>(); }
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

            var filtered = _all.Where(p =>
                (q.Length == 0 || (p.Name != null && p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                && (storage == "Storage" || (p.Storage != null && p.Storage.Equals(storage, StringComparison.OrdinalIgnoreCase)))).ToList();

            ProductList.ItemsSource = filtered;

            if (EmptyProductsStatePanel != null)
            {
                EmptyProductsStatePanel.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        private void StorageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private static ProductRow? RowOf(object sender)
        {
            return ((FrameworkElement)sender).DataContext as ProductRow;
        }

        /********************************************************************************************/
        //shows the product detail
        private async void ShowDetail(ProductRow? row, bool editable)
        {
            if (row == null) return;

            Brush overlayScrim;
            try
            {
                overlayScrim = (FindResource("ScrimDetail") as Brush) ?? new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
            }
            catch
            {
                overlayScrim = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
            }

            try
            {
                Product? prod = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(row.Name))
                        prod = await _apiClient.GetProductByIdAsync(row.Name);
                }
                catch { }

                if (prod == null && !string.IsNullOrWhiteSpace(row.Name))
                {
                    prod = _service.GetProductByName(row.Name);
                }

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
                        Id = prod.Id,
                        ProductId = prod.ProductID,
                        Name = prod.ProductName,
                        OriginalName = prod.ProductName,
                        DateAdded = "",
                        Ingredients = ingredients,
                        Method = prod.Method,
                        Storage = prod.StorageLocation
                    };
                }
                else
                {
                    // Fallback: construct a minimal ProductDetail from the product row when sample data is not available
                    detail = new SharedLibrary.Model.ProductDetail
                    {
                        Id = row.Id,
                        ProductId = row.ProductId,
                        Name = row.Name ?? string.Empty,
                        OriginalName = row.Name ?? string.Empty,
                        DateAdded = string.Empty,
                        Ingredients = new System.Collections.Generic.List<SharedLibrary.Model.IngredientLine>(),
                        Method = string.Empty,
                        Storage = row.Storage ?? string.Empty
                    };
                }

                var modal = new ProductDetailModal(detail, editable);
                modal.Saved += async () =>
                {
                    try
                    {
                        await LoadProductsAsync();
                    }
                    catch
                    {
                        var updated = _service.GetAll();
                        _all.Clear();
                        _all.AddRange(updated);
                        Refresh();
                    }
                };
                ShellWindow.Current?.ShowModal(modal, overlayScrim);
            }
            catch
            {
                // If an exception occurs, attempt to show a minimal detail modal constructed from the row
                var fallbackDetail = new SharedLibrary.Model.ProductDetail
                {
                    Id = row.Id,
                    ProductId = row.ProductId,
                    Name = row.Name ?? string.Empty,
                    OriginalName = row.Name ?? string.Empty,
                    DateAdded = string.Empty,
                    Ingredients = new System.Collections.Generic.List<SharedLibrary.Model.IngredientLine>(),
                    Method = string.Empty,
                    Storage = row.Storage ?? string.Empty
                };
                var fallbackModal = new ProductDetailModal(fallbackDetail, editable);
                fallbackModal.Saved += async () =>
                {
                    try
                    {
                        await LoadProductsAsync();
                    }
                    catch
                    {
                        var updated = _service.GetAll();
                        _all.Clear();
                        _all.AddRange(updated);
                        Refresh();
                    }
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
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var row = RowOf(sender);
            if (row == null) return;

            var answer = MessageBox.Show("Delete " + row.Name + "?", "Delete product",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            // Try API delete by name or id, fallback to local delete
            try
            {
                bool deleted = false;
                try
                {
                    string target = !string.IsNullOrWhiteSpace(row.Name) ? row.Name : row.ProductId.ToString();
                    deleted = await _apiClient.DeleteProductAsync(target);
                }
                catch { }

                if (!deleted && !string.IsNullOrWhiteSpace(row.Name))
                {
                    _service.DeleteByName(row.Name);
                }

                _all.Remove(row);
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /********************************************************************************************/
        //adds a new product to the list
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            Brush overlayScrim;
            try
            {
                overlayScrim = (FindResource("ScrimDetail") as Brush) ?? new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
            }
            catch
            {
                overlayScrim = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
            }

            var modal = new AddProductModal();
            modal.Saved += async () =>
            {
                try
                {
                    await LoadProductsAsync();
                }
                catch
                {
                    var updated = _service.GetAll();
                    _all.Clear();
                    _all.AddRange(updated);
                    Refresh();
                }
            };
            ShellWindow.Current?.ShowModal(modal, overlayScrim);
        }
    }
}
/*********************************************MAR26EOF*******************************************/