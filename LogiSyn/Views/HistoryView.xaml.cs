using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;

namespace LogiSyn.Views
{
    public partial class HistoryView : UserControl
    {
        private readonly List<OrderRow> _all;
        private bool _ready;

        public HistoryView(AppRole role)
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();
            DateHeader.Text = role == AppRole.Admin ? "Date Completed" : "Date";

            // TODO (backend): load the completed orders here
            _all = SampleData.HistoryOrders();

            DateFilter.SelectedIndex = 0;
            _ready = true;
            Refresh();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = DateFilter.SelectedItem as ComboBoxItem;
            string sort = selected == null ? "DATE" : (string)selected.Content;

            IEnumerable<OrderRow> rows = _all.Where(o =>
                q.Length == 0
                || o.Number.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || o.Customer.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

            if (sort == "Newest first") rows = rows.OrderByDescending(o => o.Date);
            else if (sort == "Oldest first") rows = rows.OrderBy(o => o.Date);

            OrderList.ItemsSource = rows.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        private void DateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            var order = ((FrameworkElement)sender).DataContext as OrderRow;
            ShellWindow.Current.Navigate("breakdown", order);
        }
    }
}