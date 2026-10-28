using System;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.ViewModels;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    // Interaction logic for HistoryView.xaml.
    // Completed order history, date filtering, and searching are managed by HistoryViewModel.
    public partial class HistoryView : UserControl
    {
        public HistoryViewModel ViewModel { get; }

        private bool _ready;

        public HistoryView(AppRole role)
        {
            InitializeComponent();

            ViewModel = new HistoryViewModel();
            DataContext = ViewModel;

            ViewModel.ConfigureRole(role);
            DateText.Text = ViewModel.DateText;
            DateHeader.Text = ViewModel.DateHeader;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HistoryViewModel.FilteredHistory))
                {
                    OrderList.ItemsSource = ViewModel.FilteredHistory;
                }
            };

            DateFilter.SelectedIndex = 0;
            _ready = true;

            _ = ViewModel.LoadHistoryAsync();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = DateFilter.SelectedItem as ComboBoxItem;
            string filter = selected == null ? "All" : (string)selected.Content;

            ViewModel.SearchQuery = q;
            ViewModel.SelectedDateFilter = filter;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

        private void DateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => Refresh();

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is OrderRow order)
            {
                ShellWindow.Current?.Navigate("breakdown", order);
            }
        }

        private void OrderRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is OrderRow order)
            {
                ShellWindow.Current?.Navigate("breakdown", order);
            }
        }
    }
}
