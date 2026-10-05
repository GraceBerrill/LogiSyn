using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;

namespace LogiSyn.Views
{

    //displays product details
    public partial class ProductDetailModal : UserControl
    {

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ProductDetailModal),
                                        new PropertyMetadata(true));

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public event System.Action? Saved;

        public ProductDetailModal(ProductDetail detail, bool editable)
        {
            InitializeComponent();

            DataContext = detail;
            IsReadOnly = !editable;
            SaveButton.Visibility = editable ? Visibility.Visible : Visibility.Collapsed;
            DateAddedText.Text = "Date Added: " + detail.DateAdded;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.CloseModal();
        }

        //save changes to product details
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var detail = DataContext as ProductDetail;
            if (detail == null) { ShellWindow.Current.CloseModal(); return; }

            var svc = new ProductService();
            var existing = svc.GetProductByName(detail.Name) ?? new Product();
            var prod = new Product
            {
                ProductID = existing.ProductID,
                ProductName = detail.Name,
                PricePerUnit = existing.PricePerUnit,
                SellBy = existing.SellBy,
                BestBefore = existing.BestBefore,
                StorageLocation = detail.Storage,
                Method = detail.Method,
                Ingredients = new System.Collections.Generic.List<IngredientRequirement>()
            };

            if (detail.Ingredients != null)
            {
                foreach (var il in detail.Ingredients)
                {
                    if (string.IsNullOrWhiteSpace(il.Name)) continue;
                    var qtyText = (il.Quantity ?? string.Empty).Trim();
                    decimal qty = 0;
                    string unit = "units";
                    if (qtyText.Length > 0)
                    {
                        var parts = qtyText.Split(' ', 2, System.StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 1 && decimal.TryParse(parts[0], out var parsed))
                        {
                            qty = parsed;
                            if (parts.Length == 2) unit = parts[1];
                        }
                        else
                        {
                            if (decimal.TryParse(qtyText, out parsed)) qty = parsed;
                            else unit = qtyText;
                        }
                    }
                    prod.Ingredients.Add(new IngredientRequirement { IngredientName = il.Name, Quantity = qty, Unit = unit });
                }
            }

            try
            {
                svc.UpdateFromDetail(prod);
                Saved?.Invoke();
            }
            catch { }

            ShellWindow.Current.CloseModal();
        }
    }
}
/*********************************************MAR26EOF*******************************************/