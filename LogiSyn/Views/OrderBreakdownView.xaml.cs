using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;
using Microsoft.Extensions.Logging;
using AndersonsBakeryAPI.Services;

namespace LogiSyn.Views
{
    public partial class OrderBreakdownView : UserControl
    {
        private readonly OrderService _orderService = new OrderService();
        private readonly ExcelOrderService _excelService = new ExcelOrderService();
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly AppRole _role;
        private readonly OrderRow? _order;
        private OrderScaled? _scaledOrder;

        //------------------------------------------------------------------------------------------------//

        public OrderBreakdownView(AppRole role, OrderRow order)
        {
            InitializeComponent();
            _role = role;
            _order = order;

            LoadBreakdown();
        }

        //------------------------------------------------------------------------------------------------//

        // Loads the order breakdown based on the user's role and order information
        private void LoadBreakdown()
        {
            string orderId = _order?.Number ?? "";
            _scaledOrder = _orderService.GetOrderById(orderId);

            if (_scaledOrder != null)
            {
                RenderOrder(_scaledOrder);
            }
            else
            {
                string title = _order == null ? "Order Breakdown" : $"{_order.Customer} Order {_order.Number}".Trim();
                if (_role == AppRole.Manager)
                {
                    var data = new SheetData { Title = title, DateText = DateTime.Now.ToString("dd MMMM yyyy") };
                    PaperHost.Content = new SheetPaper { IsReadOnly = true, DataContext = data };
                }
                else
                {
                    var data = new SummaryData { Title = title, DateText = DateTime.Now.ToString("dd MMMM yyyy") };
                    PaperHost.Content = new SummaryPaper { DataContext = data };
                }

                FetchFromApiAsync(orderId);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Fetches the order from the API asynchronously
        private async void FetchFromApiAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return;
            try
            {
                var apiOrder = await _apiClient.GetOrderByIdAsync(orderId);
                if (apiOrder != null)
                {
                    _scaledOrder = apiOrder;
                    RenderOrder(apiOrder);
                }
            }
                catch (Exception ex)
                {
                    var logger = App.ServiceProvider.GetService(typeof(Microsoft.Extensions.Logging.ILogger<OrderBreakdownView>)) as Microsoft.Extensions.Logging.ILogger<OrderBreakdownView>;
                    logger?.LogWarning(ex, "Error fetching order from API");
                }
        }

        //------------------------------------------------------------------------------------------------//

        // Renders the order based on the user's role
        private void RenderOrder(OrderScaled order)
        {
            if (_role == AppRole.Manager)
            {
                var data = SheetData.FromOrderScaled(order);
                PaperHost.Content = new SheetPaper { IsReadOnly = true, DataContext = data };
            }
            else
            {
                var data = SummaryData.FromOrderScaled(order);
                PaperHost.Content = new SummaryPaper { DataContext = data };
            }

            bool isCompleted = string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(order.Status, "Complete", StringComparison.OrdinalIgnoreCase);

            DoneButton.Content = isCompleted ? "DONE" : "ADD TO HISTORY";
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Done button click event - finalizes phase and adds order to history
        private async void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            string orderId = _order?.Number ?? "";
            var scaledOrder = _scaledOrder ?? (!string.IsNullOrWhiteSpace(orderId) ? _orderService.GetOrderById(orderId) : null);
            if (scaledOrder != null)
            {
                // Save to API first then if it fails try local async save, falling back to synchronous save as last resort
                scaledOrder.Status = "Completed";
                try
                {
                    await _apiClient.SaveOrderAsync(scaledOrder);
                }
                catch
                {
                    try
                    {
                        await _orderService.SaveOrderAsync(scaledOrder);
                    }
                    catch
                    {
                        try
                        {
                            _orderService.SaveOrder(scaledOrder);
                        }
                        catch { }
                    }
                }
            }
            else if (_order != null)
            {
                _order.Status = "Complete";
            }

            ShellWindow.Current?.Navigate("history");
        }

        //------------------------------------------------------------------------------------------------//

        // Adriaan - Event handler to email order Excel back to admin
        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            string orderId = _order?.Number ?? "";
            var scaledOrder = _scaledOrder ?? (!string.IsNullOrWhiteSpace(orderId) ? _orderService.GetOrderById(orderId) : null);
            if (scaledOrder == null)
            {
                ShellWindow.Current?.ShowToast("No active order data available to email.", isWarning: true);
                return;
            }

            try
            {
                string filePath = _excelService.ExportOrderToExcel(scaledOrder);

                bool emailSent = false;
                try
                {
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType)!;
                        dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
                        mailItem.Subject = $"Completed Production Order {scaledOrder.OrderId} - {scaledOrder.Customer}";
                        mailItem.Body = $"Hi Admin,\n\nPlease find attached the production order Excel sheet for {scaledOrder.Customer} (Order {scaledOrder.OrderId}) with actual quantities used and baker notes.\n\nKind regards,\nBakery Team";
                        mailItem.Attachments.Add(filePath);
                        mailItem.Display(false);
                        emailSent = true;
                    }
                }
                    catch (Exception comEx)
                    {
                        var logger = App.ServiceProvider.GetService(typeof(Microsoft.Extensions.Logging.ILogger<OrderBreakdownView>)) as Microsoft.Extensions.Logging.ILogger<OrderBreakdownView>;
                        logger?.LogWarning(comEx, "Direct Outlook launch unavailable");
                    }

                if (!emailSent)
                {
                    string subject = Uri.EscapeDataString($"Completed Production Order {scaledOrder.OrderId} - {scaledOrder.Customer}");
                    string body = Uri.EscapeDataString($"Hi Admin,\n\nProduction order Excel file saved at:\n{filePath}\n\nActual quantities used and notes have been recorded.");
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = $"mailto:?subject={subject}&body={body}", UseShellExecute = true });
                    }
                    catch { }

                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{filePath}\"", UseShellExecute = true });
                    ShellWindow.Current?.ShowToast("Updated order exported to Excel and opened in Explorer.");
                }
            }
            catch (Exception ex)
            {
                ShellWindow.Current?.ShowToast($"Email failed: {ex.Message}", isWarning: true);
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//