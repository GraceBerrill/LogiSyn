using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using LogiSyn.ViewModels;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    // Interaction logic for OrderSheetsView.xaml.
    // Order scaling, ingredient calculations, and persistence are managed by OrderSheetsViewModel.
    public partial class OrderSheetsView : UserControl
    {
        public OrderSheetsViewModel ViewModel { get; }

        public OrderSheetsView() : this(new OrderScaled())
        {
        }

        public OrderSheetsView(OrderScaled order)
        {
            InitializeComponent();

            ViewModel = new OrderSheetsViewModel(order);
            DataContext = ViewModel;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OrderSheetsViewModel.OrderTitle))
                {
                    TxtLeftOrderHeader.Text = ViewModel.OrderTitle;
                    TxtRightOrderHeader.Text = ViewModel.OrderTitle;
                }
                if (e.PropertyName == nameof(OrderSheetsViewModel.OrderDateText))
                {
                    TxtLeftOrderDate.Text = ViewModel.OrderDateText;
                    TxtRightOrderDate.Text = ViewModel.OrderDateText;
                }
                if (e.PropertyName == nameof(OrderSheetsViewModel.DisplayItems))
                {
                    ItemsProductionList.ItemsSource = ViewModel.DisplayItems;
                }
                if (e.PropertyName == nameof(OrderSheetsViewModel.DisplayScaling))
                {
                    ItemsScalingList.ItemsSource = ViewModel.DisplayScaling;
                }
            };

            // Initial UI sync
            TxtLeftOrderHeader.Text = ViewModel.OrderTitle;
            TxtRightOrderHeader.Text = ViewModel.OrderTitle;
            TxtLeftOrderDate.Text = ViewModel.OrderDateText;
            TxtRightOrderDate.Text = ViewModel.OrderDateText;
            ItemsProductionList.ItemsSource = ViewModel.DisplayItems;
            ItemsScalingList.ItemsSource = ViewModel.DisplayScaling;

            // Automatically persist order if it has an OrderId
            if (!string.IsNullOrWhiteSpace(order?.OrderId))
            {
                _ = ViewModel.SaveOrderAsync();
            }
        }

        private async void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveOrderAsync();
            NavigateToOrders();
        }

        private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMoreProducts
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.CreatedItem != null)
            {
                await ViewModel.AddItemAsync(dialog.CreatedItem);
            }
        }

        private async void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveOrderAsync();
            SubmitModalOverlay.Visibility = Visibility.Visible;
        }

        private async void BtnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveOrderAsync();

            var dialog = new SaveFileDialog
            {
                Title = "Save Order Production Sheet as Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Order_{ViewModel.CurrentOrder.OrderId}_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string path = ViewModel.ExportOrderToExcel(dialog.FileName);
                    MessageBox.Show($"Order successfully saved as Excel file:\n{path}", "Excel Exported",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigateToOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export Excel file: {ex.Message}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.SaveOrderAsync();
                string filePath = ViewModel.ExportOrderToExcel();

                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);

                MessageBox.Show($"Order {ViewModel.CurrentOrder.OrderId} exported as Excel and opened for printing:\n{filePath}",
                                "Print Excel", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigateToOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.SaveOrderAsync();
                string filePath = ViewModel.ExportOrderToExcel();

                bool emailSent = false;
                try
                {
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic? outlook = Activator.CreateInstance(outlookType);
                        if (outlook != null)
                        {
                            dynamic mail = outlook.CreateItem(0);
                            mail.Subject = $"Production Order Sheet - {ViewModel.CurrentOrder.OrderId} ({ViewModel.CurrentOrder.Customer})";
                            mail.Body = $"Please find attached the scaled production order sheet for {ViewModel.CurrentOrder.Customer} (Order: {ViewModel.CurrentOrder.OrderId}).";
                            mail.Attachments.Add(filePath);
                            mail.Display(false);
                            emailSent = true;
                        }
                    }
                }
                catch { }

                if (!emailSent)
                {
                    var uri = $"mailto:?subject={Uri.EscapeDataString($"Production Order Sheet - {ViewModel.CurrentOrder.OrderId}")}&body={Uri.EscapeDataString($"Order sheet exported to: {filePath}")}";
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                }

                NavigateToOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create email: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SubmitModalOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == SubmitModalOverlay)
            {
                SubmitModalOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnModalClose_Click(object sender, RoutedEventArgs e)
        {
            SubmitModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnModalSave_Click(object sender, RoutedEventArgs e)
        {
            SubmitModalOverlay.Visibility = Visibility.Collapsed;
            NavigateToOrders();
        }

        private void NavigateToOrders()
        {
            ShellWindow.Current?.Navigate("orders");
        }

        // Nested classes for complete backwards compatibility with tests and callers
        public class ProductionItemDisplayModel : ViewModels.ProductionItemDisplayModel
        {
        }

        public class ScalingItemDisplayModel : ViewModels.ScalingItemDisplayModel
        {
        }
    }
}
