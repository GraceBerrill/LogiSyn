using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class AdminOrdersView : UserControl
    {
        private readonly List<OrderRow> _all;
        private bool _ready;

        public AdminOrdersView()
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();

            // TODO (backend): load the real orders here
            _all = SampleData.Orders();

            StatusFilter.SelectedIndex = 0;
            _ready = true;
            Refresh();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = StatusFilter.SelectedItem as ComboBoxItem;
            string status = selected == null ? "All Statuses" : (string)selected.Content;

            OrderList.ItemsSource = _all.Where(o =>
                (q.Length == 0
                    || o.Number.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || o.Customer.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                && (status == "All Statuses" || o.Status == status)).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Refresh();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void LinkOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.Navigate("ordersheets");
        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.Navigate("createorder");
        }
    }
}