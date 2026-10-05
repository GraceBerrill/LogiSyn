using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AndersonsBakeryAPI.Services;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class EmailOrdersModal : UserControl
    {
        private readonly List<OrderSelectionItem> _items = new();
        private readonly ExcelOrderService _excelService = new();
        private readonly OrderService _orderService = new();
        private bool _isCustomSubject = false;

        public EmailOrdersModal(IEnumerable<OrderScaled> orders, string? preselectOrderId = null)
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

            NotesBox.Text = "Please find attached the production order Excel sheet(s) with full product breakdowns, ingredients, packaging, and raw material scaling.\n\nPlease record actual used quantities and baker notes in the highlighted columns and upload when complete.";

            SubjectBox.TextChanged += (s, e) =>
            {
                if (SubjectBox.IsFocused)
                {
                    _isCustomSubject = true;
                }
            };

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
            SendEmailButton.Content = selected > 0 ? $"Email Selected ({selected})" : "Create Email";
            SendEmailButton.IsEnabled = selected > 0;

            SelectAllBox.IsChecked = total > 0 && selected == total
                ? true
                : (selected > 0 ? null : false);

            if (!_isCustomSubject)
            {
                var selectedOrders = _items.Where(i => i.IsSelected).Select(i => i.Order).ToList();
                if (selectedOrders.Count == 0)
                {
                    SubjectBox.Text = "Production Orders - LogiSyn";
                }
                else if (selectedOrders.Count == 1)
                {
                    SubjectBox.Text = $"Production Order {selectedOrders[0].OrderId} - {selectedOrders[0].Customer}";
                }
                else
                {
                    var summary = string.Join(", ", selectedOrders.Take(3).Select(o => $"{o.OrderId} {o.Customer}"));
                    if (selectedOrders.Count > 3) summary += $" +{selectedOrders.Count - 3} more";
                    SubjectBox.Text = $"Production Orders ({summary})";
                }
            }
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

        private void SendEmailButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrders = _items.Where(i => i.IsSelected).Select(i => i.Order).ToList();
            if (selectedOrders.Count == 0)
            {
                MessageBox.Show("Please select at least one order to email.", "No Orders Selected",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Export each selected order as an Excel file
                var generatedFiles = new List<string>();
                foreach (var order in selectedOrders)
                {
                    string filePath = _excelService.ExportOrderToExcel(order);
                    generatedFiles.Add(filePath);
                }

                // If multiple orders, also generate a combined workbook with all orders
                if (selectedOrders.Count > 1)
                {
                    try
                    {
                        string combinedPath = _excelService.ExportOrdersToExcel(selectedOrders);
                        generatedFiles.Insert(0, combinedPath);
                    }
                    catch { }
                }

                // Build rich email body
                var sb = new StringBuilder();
                sb.AppendLine(NotesBox.Text.Trim());
                sb.AppendLine();
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine($"SUMMARY OF ORDERS ({selectedOrders.Count})");
                sb.AppendLine("--------------------------------------------------");

                foreach (var order in selectedOrders)
                {
                    try
                    {
                        var emailModel = _orderService.BuildScalingSheetEmail(order);
                        sb.AppendLine();
                        sb.AppendLine(emailModel.Body);
                        sb.AppendLine("--------------------------------------------------");
                    }
                    catch
                    {
                        sb.AppendLine($"Order: {order.OrderId} - {order.Customer} ({order.Status})");
                        sb.AppendLine($"Date: {order.OrderDate:dd/MM/yyyy}");
                        foreach (var p in order.productionItems ?? new List<ProductionItem>())
                        {
                            sb.AppendLine($"  • {p.Amount} {p.ProductName} [{p.ProductionLine}] - Pans: {p.packaging?.Pans}, Trolleys: {p.packaging?.Trolleys}");
                        }
                    }
                }

                string fullBody = sb.ToString();
                string subject = SubjectBox.Text.Trim();
                string recipient = RecipientBox.Text.Trim();

                bool emailSent = false;
                try
                {
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType)!;
                        dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
                        if (!string.IsNullOrWhiteSpace(recipient))
                        {
                            mailItem.To = recipient;
                        }
                        mailItem.Subject = subject;
                        mailItem.Body = fullBody;

                        foreach (var file in generatedFiles)
                        {
                            if (File.Exists(file))
                            {
                                mailItem.Attachments.Add(file);
                            }
                        }

                        mailItem.Display(false);
                        emailSent = true;
                    }
                }
                catch (Exception comEx)
                {
                    Console.WriteLine($"[Outlook COM] Direct Outlook launch unavailable: {comEx.Message}");
                }

                CloseModal();

                if (emailSent)
                {
                    MessageBox.Show(
                        $"Outlook email draft created successfully with {generatedFiles.Count} Excel attachment(s) for {selectedOrders.Count} order(s)!",
                        "Email Created",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Fallback to mailto link
                    string subjectEncoded = Uri.EscapeDataString(subject);
                    string bodyTruncated = fullBody.Length > 1200
                        ? fullBody.Substring(0, 1200) + "\n\n[Full order production sheets attached in Excel file(s)]"
                        : fullBody;
                    string bodyEncoded = Uri.EscapeDataString(bodyTruncated);
                    string toEncoded = Uri.EscapeDataString(recipient);

                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"mailto:{toEncoded}?subject={subjectEncoded}&body={bodyEncoded}",
                            UseShellExecute = true
                        });
                    }
                    catch { }

                    if (generatedFiles.Count > 0 && File.Exists(generatedFiles[0]))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{generatedFiles[0]}\"",
                            UseShellExecute = true
                        });
                    }

                    MessageBox.Show(
                        $"Email draft launched!\n\nProduction order Excel file(s) generated:\n{string.Join("\n", generatedFiles)}\n\nPlease attach the highlighted Excel file(s) to your email draft.",
                        "Email Prepared",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Email action failed: {ex.Message}", "Email Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

