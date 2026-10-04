<<<<<<< HEAD
﻿using SharedLibrary.Model;
=======
// Adriaan
using LogiSyn.Interface;
using LogiSyn.Model;
using LogiSyn.Services;
>>>>>>> Adriaan
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for AdminOrderBreakdown.xaml
    /// </summary>
    public partial class AdminOrderBreakdown : Page
    {
        private readonly OrderScaled _order;
        private readonly IOrderService _orderService = new OrderService();
        private readonly ExcelOrderService _excelService = new ExcelOrderService();
        private readonly ApiClient _apiClient = new ApiClient();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the AdminOrderBreakdown class
        public AdminOrderBreakdown(OrderScaled order)
        {
            InitializeComponent();

            _order = order ?? throw new ArgumentNullException(nameof(order));

            PopulateUI();
        }

        //------------------------------------------------------------------------------------------------//

        private void PopulateUI()
        {
            TxtOrderTitle.Text = $"{_order.Customer} Order {_order.OrderId}".Trim();
            TxtOrderDate.Text = _order.OrderDate.ToString("d MMMM yyyy");
            TxtStatus.Text = string.IsNullOrWhiteSpace(_order.Status) ? "COMPLETED" : _order.Status.ToUpper();

            bool isCompleted = string.Equals(_order.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(_order.Status, "Complete", StringComparison.OrdinalIgnoreCase);

            if (BtnDone != null)
            {
                BtnDone.Content = isCompleted ? "DONE" : "COMPLETE & ADD TO HISTORY";
            }

            // Prepare the list of items for display in the breakdown
            var itemsList = (_order.productionItems ?? new List<ProductionItem>())
                .Select(item =>
                {
                    // Extract the product name and amount from the ProductName property
                    string rawName = item.ProductName ?? string.Empty;
                    string amount = string.Empty;
                    string name = rawName;

                    var parts = rawName.Split(' ', 2);
                    if (parts.Length == 2 && int.TryParse(parts[0], out _))
                    {
                        amount = parts[0];
                        name = parts[1];
                    }
                    else
                    {
                        amount = "100";
                    }

                    var pkg = item.packaging ?? new Packaging();

                    // Create a new BreakdownProductItemViewModel for each production item
                    return new BreakdownProductItemViewModel
                    {
                        DisplayProductName = name,
                        DisplayAmount = amount,
                        ProductionLine = item.ProductionLine ?? "Production 1",
                        PansAmount = pkg.Pans.ToString(),
                        PansUsed = (pkg.PansUsed > 0 ? pkg.PansUsed : pkg.Pans).ToString(),
                        TrolleysAmount = pkg.Trolleys.ToString(),
                        TrolleysUsed = (pkg.TrolleysUsed > 0 ? pkg.TrolleysUsed : pkg.Trolleys).ToString(),
                        Notes = string.IsNullOrWhiteSpace(item.Notes) ? "No additional notes recorded." : item.Notes,
                        Ingredients = (item.ReqIngredients ?? new List<Ingredients>()).Select(ing => new BreakdownIngredientItemViewModel
                        {
                            IngredientName = ing.IngredientName,
                            Amount = $"{ing.IngredientAmount} {ing.MeasuredIngredient}",
                            Additional = $"{ing.AdditionsAmount} {ing.MeasuredIngredient}",
                            Used = $"{(ing.AmountUsed > 0 ? ing.AmountUsed : ing.IngredientAmount)} {ing.MeasuredIngredient}"
                        }).ToList()
                    };
                }).ToList();

            ItemsBreakdownList.ItemsSource = itemsList;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "Done" button click event - completes phase and adds to history
        private async void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            if (_order != null)
            {
                _order.Status = "Completed";
                _orderService.SaveOrder(_order);

                try
                {
                    await _apiClient.SaveOrderAsync(_order);
                    await _apiClient.UpdateOrderStatusAsync(_order.OrderId, "Completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AdminOrderBreakdown] Sync error: {ex.Message}");
                }
            }

            NavigationService?.Navigate(new AdminOrderHistory());
        }

        //------------------------------------------------------------------------------------------------//

        // Adriaan - Event handler to email order Excel
        /// <summary>
        /// Handles exporting the current order to an Excel spreadsheet and dispatching it via email.
        /// Attempts direct automation via Microsoft Outlook COM first, with a fallback to the default mail client and Windows Explorer.
        /// </summary>
        private void BtnEmail_Click(object sender, RoutedEventArgs e)
        {
            // Verify that active order data exists before proceeding
            if (_order == null)
            {
                MessageBox.Show("No active order data available to email.", "Email Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Export order details to an Excel spreadsheet and retrieve the generated file path
                string filePath = _excelService.ExportOrderToExcel(_order);

                bool emailSent = false;
                try
                {
                    // Attempt direct email composition via Microsoft Outlook COM automation
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType)!;
                        dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
                        mailItem.Subject = $"Production Order {_order.OrderId} - {_order.Customer}";
                        mailItem.Body = $"Please find attached the production order Excel sheet for {_order.Customer} (Order {_order.OrderId}).\n\nKind regards,\nLogiSyn System";
                        mailItem.Attachments.Add(filePath);
                        mailItem.Display(false); // Display Outlook email window without modal blocking
                        emailSent = true;
                    }
                }
                catch (Exception comEx)
                {
                    // Outlook COM automation may fail if Outlook is not installed or permissions are restricted
                    Console.WriteLine($"[Outlook COM] Direct Outlook launch unavailable: {comEx.Message}");
                }

                // Fallback: If Outlook COM is unavailable, launch default mail client and open file explorer
                if (!emailSent)
                {
                    string subject = Uri.EscapeDataString($"Production Order {_order.OrderId} - {_order.Customer}");
                    string body = Uri.EscapeDataString($"Production order Excel file saved at:\n{filePath}");
                    
                    try
                    {
                        // Open default mail client with pre-filled subject and body
                        Process.Start(new ProcessStartInfo { FileName = $"mailto:?subject={subject}&body={body}", UseShellExecute = true });
                    }
                    catch { }

                    // Open Windows Explorer highlighting the exported Excel file so user can drag-and-drop or attach it
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{filePath}\"", UseShellExecute = true });
                    MessageBox.Show($"Order exported to Excel:\n{filePath}\n\nPlease attach this file to your email draft.", "Email Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // Display error prompt if export or general emailing process fails
                MessageBox.Show($"Email failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    //------------------------------------------------------------------------------------------------//

    // ViewModel for displaying product breakdown information
    public class BreakdownProductItemViewModel
    {
        public string DisplayProductName { get; set; } = string.Empty;
        public string DisplayAmount { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public string PansAmount { get; set; } = string.Empty;
        public string PansUsed { get; set; } = string.Empty;
        public string TrolleysAmount { get; set; } = string.Empty;
        public string TrolleysUsed { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<BreakdownIngredientItemViewModel> Ingredients { get; set; } = new();
    }

    //------------------------------------------------------------------------------------------------//

    // ViewModel for displaying ingredient breakdown information
    public class BreakdownIngredientItemViewModel
    {
        public string IngredientName { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Additional { get; set; } = string.Empty;
        public string Used { get; set; } = string.Empty;
    }
}

//--------------------------------------End of File----------------------------------------------------------//
