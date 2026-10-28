using System;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.ViewModels;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    // Interaction logic for UserOrderView.xaml.
    // Order loading and Excel import processing are managed by UserOrdersViewModel.
    public partial class UserOrdersView : UserControl
    {
        public UserOrdersViewModel ViewModel { get; }

        public UserOrdersView()
        {
            InitializeComponent();

            ViewModel = new UserOrdersViewModel();
            DataContext = ViewModel;

            DateText.Text = ViewModel.DateText;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UserOrdersViewModel.Orders))
                {
                    OrderList.ItemsSource = ViewModel.Orders;
                }
            };

            _ = ViewModel.LoadOrdersAsync();
        }

        private async void BtnUploadExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Upload Production Excel Sheet",
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var parsed = await ViewModel.ProcessUploadedExcelAsync(dialog.FileName);
                    if (parsed == null)
                    {
                        MessageBox.Show("Could not read valid production order data from the selected Excel file.",
                                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var res = MessageBox.Show(
                        $"Excel sheet for Order {parsed.OrderId} imported successfully!\nUsed quantities and notes have been populated.\n\nDo you want to mark this order as Completed now?",
                        "Excel Imported",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        parsed.Status = "Completed";
                        var client = new AndersonsBakeryAPI.Services.ApiClient();
                        var ordService = new AndersonsBakeryAPI.Services.OrderService();
                        try { await client.SaveOrderAsync(parsed); } catch { await ordService.SaveOrderAsync(parsed); }

                        await ViewModel.LoadOrdersAsync();
                        MessageBox.Show($"Order {parsed.OrderId} marked as Completed!", "Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (res == MessageBoxResult.No)
                    {
                        var client = new AndersonsBakeryAPI.Services.ApiClient();
                        var ordService = new AndersonsBakeryAPI.Services.OrderService();
                        try { await client.SaveOrderAsync(parsed); } catch { await ordService.SaveOrderAsync(parsed); }

                        await ViewModel.LoadOrdersAsync();
                        ShellWindow.Current?.Navigate("usersheet", OrderRow.FromOrderScaled(parsed));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import Excel: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OrderRow_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is OrderRow order)
            {
                ShellWindow.Current?.Navigate("usersheet", order);
            }
        }
    }
}
