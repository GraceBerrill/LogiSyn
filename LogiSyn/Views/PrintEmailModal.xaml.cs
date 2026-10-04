// Adriaan
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class PrintEmailModal : UserControl
    {
        private readonly OrderScaled? _order;
        private readonly ExcelOrderService _excelService = new ExcelOrderService();
        private readonly OrderService _orderService = new OrderService();

        //------------------------------------------------------------------------------------------------//

        // Constructor for class
        public PrintEmailModal(OrderScaled? order = null)
        {
            InitializeComponent();
            _order = order ?? _orderService.GetOrders().LastOrDefault();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for Print button click
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current?.CloseModal();
            try
            {
                // Get the order to print, either from the provided order or the last order in the service
                var order = _order ?? _orderService.GetOrders().LastOrDefault();
                if (order == null)
                {
                    MessageBox.Show("No active order available to print.", "Print Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Export the order to Excel and open it for printing
                string path = _excelService.ExportOrderToExcel(order);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                MessageBox.Show($"Order {order.OrderId} exported as Excel and opened for printing:\n{path}", "Print Excel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for Email button click
        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current?.CloseModal();
            try
            {
                // Get the order to email, either from the provided order or the last order in the service
                var order = _order ?? _orderService.GetOrders().LastOrDefault();
                if (order == null)
                {
                    MessageBox.Show("No active order available to email.", "Email Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string path = _excelService.ExportOrderToExcel(order);
                bool emailSent = false;
                try
                {
                    // Attempt to create and display an Outlook email via late-bound COM (no office.dll dependency)
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType)!;
                        dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
                        mailItem.Subject = $"Production Order {order.OrderId} - {order.Customer}";
                        mailItem.Body = $"Please find attached the production order Excel sheet for {order.Customer} (Order {order.OrderId}).\n\nPlease enter used quantities and notes in the highlighted columns and upload when complete.";
                        mailItem.Attachments.Add(path);
                        mailItem.Display(false);
                        emailSent = true;
                    }
                }
                catch (Exception comEx)
                {
                    Console.WriteLine($"[Outlook COM] Direct Outlook launch unavailable: {comEx.Message}");
                }

                if (!emailSent)
                {
                    // If Outlook is not available, fallback to using the default mail client with a mailto link
                    string subject = Uri.EscapeDataString($"Production Order {order.OrderId} - {order.Customer}");
                    string body = Uri.EscapeDataString($"Production order Excel file saved at:\n{path}\n\nPlease enter used quantities and notes in the highlighted columns.");
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = $"mailto:?subject={subject}&body={body}", UseShellExecute = true });
                    }
                    catch { }

                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{path}\"", UseShellExecute = true });
                    MessageBox.Show($"Order exported to Excel:\n{path}\n\nPlease attach this file to your email draft.", "Email Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Email failed: {ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//