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

        //add a product to the list and close the model
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductBox.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please enter the product name.", "Add Product",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var svc = new LogiSyn.Services.ProductService();
            var storage = string.Empty;
            try { storage = (StorageBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty; } catch { }

            var row = new LogiSyn.Model.ProductRow
            {
                Name = ProductBox.Text.Trim(),
                Price = ProductPriceBox?.Text.Trim() ?? string.Empty,
                SellBy = SellByBox?.Text.Trim() ?? string.Empty,
                BestBefore = BestBeforeBox?.Text.Trim() ?? string.Empty,
                Storage = storage
            };

            try
            {
                svc.Add(row);
            }
            catch { }

            ShellWindow.Current.CloseModal();
        }
    }
}
/*********************************************MAR26EOF*******************************************/