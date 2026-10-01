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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductBox.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please enter the product name.", "Add Product",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // TODO (backend): add this product (ProductBox, ProductAmountBox, ProductionBox,
            // IngredientBox ... PackagingUsedBox) to the order, then refresh the sheets page.
            ShellWindow.Current.CloseModal();
        }
    }
}