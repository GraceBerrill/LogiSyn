using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LogiSyn.ViewModels;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    // Interaction logic for AdminOrdersView.xaml.
    // Order filtering and state management are handled by AdminOrdersViewModel.
    public partial class AdminOrdersView : UserControl
    {
        public AdminOrdersViewModel ViewModel { get; }

        private readonly DispatcherTimer _refreshTimer;
        private bool _ready;

        public AdminOrdersView()
        {
            InitializeComponent();

            ViewModel = new AdminOrdersViewModel();
            DataContext = ViewModel;

            DateText.Text = ViewModel.DateText;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AdminOrdersViewModel.FilteredOrders))
                {
                    OrderList.ItemsSource = ViewModel.FilteredOrders;
                    if (EmptyStatePanel != null)
                    {
                        EmptyStatePanel.Visibility = ViewModel.FilteredOrders.Count == 0
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                }
            };

            StatusFilter.SelectedIndex = 0;
            _ready = true;

            _ = ViewModel.LoadOrdersAsync();

            // Set up auto-refresh timer so that orders are refreshed every 10 seconds
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _refreshTimer.Tick += (s, e) => _ = ViewModel.LoadOrdersAsync();
            _refreshTimer.Start();

            Unloaded += (s, e) => _refreshTimer.Stop();
        }

        private void Refresh()
        {
            if (!_ready) return;

            string q = SearchBox.Text.Trim();
            var selected = StatusFilter.SelectedItem as ComboBoxItem;
            string status = selected == null ? "All Statuses" : (string)selected.Content;

            ViewModel.SearchQuery = q;
            ViewModel.SelectedStatusFilter = status;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => Refresh();

        private void LinkOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.AllOrders.Count > 0)
            {
                ShellWindow.Current?.Navigate("breakdown", ViewModel.AllOrders[0]);
            }
            else
            {
                ShellWindow.Current?.Navigate("createorder");
            }
        }

        private void BtnViewOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is OrderRow order)
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

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShellWindow.Current != null)
            {
                ShellWindow.Current.Navigate("createorder");
                return;
            }

            var parentWindow = Window.GetWindow(this);
            if (parentWindow?.FindName("MainContent") is ContentControl mainContent)
            {
                mainContent.Content = new CreateOrderView();
                return;
            }

            MessageBox.Show($"Could not find navigation container. Active window is: {parentWindow?.GetType().Name}");
        }

        public void RefreshOrders()
        {
            _ = ViewModel.LoadOrdersAsync();
        }
    }
}
