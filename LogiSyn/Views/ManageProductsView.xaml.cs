using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class ManageProductsView : UserControl
    {
        private readonly List<ProductRow> _all;
        private readonly LogiSyn.Services.ProductService _service = new LogiSyn.Services.ProductService();
        private bool _ready;

        public ManageProductsView()
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();
            _all = _service.GetAll();

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
                LogiSyn.Model.ProductDetail detail;
                if (prod != null)
                {
                    var ingredients = new System.Collections.Generic.List<LogiSyn.Model.IngredientLine>();
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

                            ingredients.Add(new LogiSyn.Model.IngredientLine { Name = ing.IngredientName, Quantity = qtyText });
                        }
                    }

                    detail = new LogiSyn.Model.ProductDetail
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

                ShellWindow.Current?.ShowModal(new ProductDetailModal(detail, editable), overlayScrim);
                var updated = _service.GetAll();
                _all.Clear();
                _all.AddRange(updated);
                Refresh();
            }
            catch
            {
                ShellWindow.Current?.ShowModal(new ProductDetailModal(SampleData.DetailFor(row), editable), overlayScrim);
                try
                {
                    var updated = _service.GetAll();
                    _all.Clear();
                    _all.AddRange(updated);
                    Refresh();
                }
                catch { }
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

            //delete via service then refresh
            try { _service.DeleteByName(row.Name); }
            catch { }
            _all.Remove(row);
            Refresh();
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

            ShellWindow.Current?.ShowModal(new AddProductModal(), overlayScrim);

            try
            {
                var updated = _service.GetAll();
                _all.Clear();
                _all.AddRange(updated);
                Refresh();
            }
            catch { }
        }
    }
}
/*********************************************MAR26EOF*******************************************/