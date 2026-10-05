using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AndersonsBakeryAPI.Services;
using Microsoft.Win32;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class ExportOrdersModal : UserControl
    {
        private readonly List<OrderSelectionItem> _items = new();
        private readonly ExcelOrderService _excelService = new();

        public ExportOrdersModal(IEnumerable<OrderScaled> orders, string? preselectOrderId = null)
        {
            InitializeComponent();

            var orderList = orders?.ToList() ?? new List<OrderScaled>();
            foreach (var ord in orderList)
            {
                bool select = string.IsNullOrWhiteSpace(preselectOrderId)
                    ? true
                    : string.Equals(ord.OrderId, preselectOrderId, StringComparison.OrdinalIgnoreCase);

                var item = new OrderSelectionItem
                {
                    Order = ord,
                    IsSelected = select
                };
                item.PropertyChanged += Item_PropertyChanged;
                _items.Add(item);
            }

            OrdersList.ItemsSource = _items;
            UpdateSelectionSummary();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderSelectionItem.IsSelected))
            {
                UpdateSelectionSummary();
            }
        }

        private void UpdateSelectionSummary()
        {
            int selected = _items.Count(i => i.IsSelected);
            int total = _items.Count;

            SelectedCountText.Text = $"{selected} of {total} orders selected";
            ExportButton.Content = selected > 0 ? $"Export Selected ({selected})" : "Export to Excel";
            ExportButton.IsEnabled = selected > 0;

            SelectAllBox.IsChecked = total > 0 && selected == total
                ? true
                : (selected > 0 ? null : false);
        }

        private void SelectAllBox_Click(object sender, RoutedEventArgs e)
        {
            bool check = SelectAllBox.IsChecked == true;
            foreach (var item in _items)
            {
                item.IsSelected = check;
            }
            UpdateSelectionSummary();
        }

        private void ItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectionSummary();
        }

        private void Row_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is CheckBox) return;

            if (sender is FrameworkElement elem && elem.DataContext is OrderSelectionItem item)
            {
                item.IsSelected = !item.IsSelected;
                UpdateSelectionSummary();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseModal();
        }

        private void CloseModal()
        {
            if (ShellWindow.Current != null)
            {
                ShellWindow.Current.CloseModal();
                return;
            }

            var win = Window.GetWindow(this);
            win?.Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrders = _items.Where(i => i.IsSelected).Select(i => i.Order).ToList();
            if (selectedOrders.Count == 0)
            {
                MessageBox.Show("Please select at least one order to export.", "No Orders Selected",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (selectedOrders.Count == 1)
                {
                    var order = selectedOrders[0];
                    string safeId = string.Join("_", (order.OrderId ?? "001").Split(Path.GetInvalidFileNameChars()));
                    string safeCustomer = string.Join("_", (order.Customer ?? "Customer").Split(Path.GetInvalidFileNameChars()));

                    var dialog = new SaveFileDialog
                    {
                        Title = $"Export Order {order.OrderId} to Excel",
                        Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                        FileName = $"Order_{safeId}_{safeCustomer}_{DateTime.Now:yyyyMMdd}.xlsx"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        string path = _excelService.ExportOrderToExcel(order, dialog.FileName);
                        CloseModal();

                        var res = MessageBox.Show(
                            $"Order {order.OrderId} ({order.Customer}) exported successfully to Excel!\n\nFile location:\n{path}\n\nWould you like to open this Excel file now?",
                            "Excel Export Complete",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (res == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                        }
                    }
                }
                else
                {
                    // Multiple orders
                    if (RbSeparate.IsChecked == true)
                    {
                        var folderDialog = new OpenFolderDialog
                        {
                            Title = "Select Folder to Save Excel Orders"
                        };

                        if (folderDialog.ShowDialog() == true)
                        {
                            string folder = folderDialog.FolderName;
                            var files = _excelService.ExportOrdersToIndividualFiles(selectedOrders, folder);
                            CloseModal();

                            var res = MessageBox.Show(
                                $"Successfully exported {files.Count} production orders to:\n{folder}\n\nWould you like to view the files in Windows Explorer?",
                                "Export Complete",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Information);

                            if (res == MessageBoxResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
                            }
                        }
                    }
                    else
                    {
                        // Single combined workbook
                        var dialog = new SaveFileDialog
                        {
                            Title = $"Export {selectedOrders.Count} Orders to Excel Workbook",
                            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                            FileName = $"Production_Orders_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            string path = _excelService.ExportOrdersToExcel(selectedOrders, dialog.FileName);
                            CloseModal();

                            var res = MessageBox.Show(
                                $"Successfully exported {selectedOrders.Count} production orders to Excel!\nOverview sheet and dedicated tabs for each order have been generated.\n\nFile location:\n{path}\n\nWould you like to open the workbook now?",
                                "Excel Export Complete",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Information);

                            if (res == MessageBoxResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel export failed: {ex.Message}", "Export Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class OrderSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public OrderScaled Order { get; set; } = null!;
        public string Number => Order.OrderId;
        public string Customer => Order.Customer;
        public string DateText => Order.OrderDate.ToString("dd/MM/yyyy");
        public string Status => string.IsNullOrWhiteSpace(Order.Status) ? "Pending" : Order.Status;

        public bool IsComplete =>
            string.Equals(Status, "Complete", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase);

        public Brush StatusBrush => IsComplete
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#148C45"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C29D70"));

        public Brush StatusBgBrush => IsComplete
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));

        public string ItemsSummary
        {
            get
            {
                var p = Order.productionItems ?? new List<ProductionItem>();
                if (p.Count == 0) return "No products listed";
                var names = string.Join(", ", p.Take(2).Select(i => $"{i.Amount} {i.ProductName}"));
                if (p.Count > 2) names += $", +{p.Count - 2} more";
                return $"{p.Count} item{(p.Count > 1 ? "s" : "")} ({names})";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}

