using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogiSyn.ViewModels;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    // Interaction logic for ManageProductsView.xaml.
    // Catalog retrieval, search, storage filtering, and deletion are managed by ManageProductsViewModel.
    public partial class ManageProductsView : UserControl
    {
        public ManageProductsViewModel ViewModel { get; }

        private bool _ready;

        public ManageProductsView()
        {
            InitializeComponent();

            ViewModel = new ManageProductsViewModel();
            DataContext = ViewModel;

            DateText.Text = ViewModel.DateText;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ManageProductsViewModel.FilteredProducts))
                {
                    ProductList.ItemsSource = ViewModel.FilteredProducts;
                    if (EmptyProductsStatePanel != null)
                    {
                        EmptyProductsStatePanel.Visibility = ViewModel.FilteredProducts.Count == 0
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                }
                if (e.PropertyName == nameof(ManageProductsViewModel.IsLoading))
                {
                    LoadingText.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                }
            };

            StorageFilter.SelectedIndex = 0;
            _ready = true;

            Loaded += async (s, e) => await ViewModel.LoadProductsAsync();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = StorageFilter.SelectedItem as ComboBoxItem;
            string storage = selected == null ? "Storage" : (string)selected.Content;

            ViewModel.SearchQuery = q;
            ViewModel.SelectedStorageFilter = storage;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

        private void StorageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => Refresh();

        private static ProductRow? RowOf(object sender)
        {
            return ((FrameworkElement)sender).DataContext as ProductRow;
        }

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

            var detail = await ViewModel.GetProductDetailAsync(row);
            var modal = new ProductDetailModal(detail, editable);
            modal.Saved += async () => await ViewModel.LoadProductsAsync();

            ShellWindow.Current?.ShowModal(modal, overlayScrim);
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDetail(RowOf(sender), false);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDetail(RowOf(sender), true);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var row = RowOf(sender);
            if (row == null) return;

            var answer = MessageBox.Show("Delete " + row.Name + "?", "Delete product",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                await ViewModel.DeleteProductAsync(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
            modal.Saved += async () => await ViewModel.LoadProductsAsync();
            ShellWindow.Current?.ShowModal(modal, overlayScrim);
        }
    }
}
