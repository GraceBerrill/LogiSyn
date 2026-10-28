using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogiSyn.ViewModels;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    // Interaction logic for DashboardView.xaml.
    // Presentation logic and data management are handled by DashboardViewModel.
    public partial class DashboardView : UserControl
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardView(SharedLibrary.Model.AppRole role)
        {
            InitializeComponent();

            ViewModel = new DashboardViewModel();
            DataContext = ViewModel;

            // Sync ViewModel properties to named XAML elements for complete backwards compatibility
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DashboardViewModel.RecentOrders))
                {
                    RecentList.ItemsSource = ViewModel.RecentOrders;
                }
                if (e.PropertyName == nameof(DashboardViewModel.NewOrdersValue))
                {
                    NewOrdersValue.Text = ViewModel.NewOrdersValue;
                }
                if (e.PropertyName == nameof(DashboardViewModel.CompletedValue))
                {
                    CompletedValue.Text = ViewModel.CompletedValue;
                }
                if (e.PropertyName == nameof(DashboardViewModel.DateText))
                {
                    DateText.Text = ViewModel.DateText;
                }
                if (e.PropertyName == nameof(DashboardViewModel.NewOrdersLabel))
                {
                    NewOrdersLabel.Text = ViewModel.NewOrdersLabel;
                }
                if (e.PropertyName == nameof(DashboardViewModel.ExportButtonsVisibility))
                {
                    ExportButtons.Visibility = ViewModel.ExportButtonsVisibility;
                }
                if (e.PropertyName == nameof(DashboardViewModel.NoOrdersVisibility))
                {
                    NoOrdersText.Visibility = ViewModel.NoOrdersVisibility;
                }
            };

            ConfigureRole(role.ToString());
        }

        public void ConfigureRole(string role)
        {
            ViewModel.ConfigureRole(role);
        }

        public async Task<List<OrderScaled>> GetOrdersScaledAsync()
        {
            return await ViewModel.GetOrdersScaledAsync();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.CurrentOrdersScaled.Count == 0)
                {
                    ShellWindow.Current?.ShowToast("No orders available to export.", isWarning: true);
                    return;
                }

                var modal = new ExportOrdersModal(ViewModel.CurrentOrdersScaled);

                if (ShellWindow.Current != null)
                {
                    ShellWindow.Current.ShowModal(modal, (Brush)FindResource("ScrimDetail"));
                }
                else
                {
                    var win = new Window
                    {
                        Content = modal,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Title = "Export Orders to Excel"
                    };
                    win.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ShellWindow.Current?.ShowToast($"Failed to open Excel export: {ex.Message}", isWarning: true);
            }
        }

        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.CurrentOrdersScaled.Count == 0)
                {
                    ShellWindow.Current?.ShowToast("No orders available to email.", isWarning: true);
                    return;
                }

                var modal = new EmailOrdersModal(ViewModel.CurrentOrdersScaled);

                if (ShellWindow.Current != null)
                {
                    ShellWindow.Current.ShowModal(modal, (Brush)FindResource("ScrimDetail"));
                }
                else
                {
                    var win = new Window
                    {
                        Content = modal,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Title = "Email Orders"
                    };
                    win.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ShellWindow.Current?.ShowToast($"Failed to open Email modal: {ex.Message}", isWarning: true);
            }
        }

        private void RecentOrderRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is DashboardOrderItem item)
            {
                var row = ViewModel.GetOrderRowForItem(item);
                ShellWindow.Current?.Navigate("breakdown", row);
            }
        }
    }
}
