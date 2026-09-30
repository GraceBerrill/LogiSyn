using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogiSyn.Model;

namespace LogiSyn.Views
{
    public partial class ManageProductsView : UserControl
    {
        private readonly List<ProductRow> _all;
        private bool _ready;

        public ManageProductsView()
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();

            // TODO (backend): load the real products here
            _all = SampleData.Products();

            StorageFilter.SelectedIndex = 0;
            _ready = true;
            Refresh();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = StorageFilter.SelectedItem as ComboBoxItem;
            string storage = selected == null ? "Storage" : (string)selected.Content;

            ProductList.ItemsSource = _all.Where(p =>
                (q.Length == 0 || p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                && (storage == "Storage" || p.Storage == storage)).ToList();
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

        private void ShowDetail(ProductRow row, bool editable)
        {
            if (row == null) return;
            ShellWindow.Current.ShowModal(
                new ProductDetailModal(SampleData.DetailFor(row), editable),
                (Brush)FindResource("ScrimDetail"));
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDetail(RowOf(sender), false);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDetail(RowOf(sender), true);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var row = RowOf(sender);
            if (row == null) return;

            var answer = MessageBox.Show("Delete " + row.Name + "?", "Delete product",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            // TODO (backend): delete the product in the database
            _all.Remove(row);
            Refresh();
        }
    }
}