// Adriaan
using LogiSyn.Interface;
using LogiSyn.Model;
using LogiSyn.Services;
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
    /// Interaction logic for UserOrderPageReview.xaml
    /// </summary>
    public partial class UserOrderPageReview : Page
    {
        private readonly OrderScaled _order;
        private readonly IOrderService _orderService;
        private readonly ExcelOrderService _excelService = new ExcelOrderService();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the UserOrderPageReview class
        public UserOrderPageReview(OrderScaled order)
        {
            InitializeComponent();
            _order = order ?? throw new ArgumentNullException(nameof(order));
            _orderService = new OrderService();

            PopulateUI();
        }

        //------------------------------------------------------------------------------------------------//

        private List<UserProductionItemViewModel> _presentationItems = new();

        private void PopulateUI()
        {
            TxtOrderTitle.Text = $"{_order.Customer} Order {_order.OrderId}".Trim();
            TxtOrderDate.Text = _order.OrderDate.ToString("d MMMM yyyy");

            // Wrap items with editable presentation bindings
            _presentationItems = _order.productionItems.Select(item => new UserProductionItemViewModel
            {
                ProductName = item.ProductName,
                Amount = item.Amount > 0 ? item.Amount.ToString() : "100",
                ProductionLine = item.ProductionLine,
                Packaging = item.packaging,
                Notes = item.Notes ?? string.Empty,
                Ingredients = item.ReqIngredients.Select(ing => new UserIngredientViewModel
                {
                    IngredientName = ing.IngredientName,
                    DisplayAmount = $"{ing.IngredientAmount} {ing.MeasuredIngredient}".Trim(),
                    DisplayAdditional = $"{ing.AdditionsAmount} {ing.MeasuredIngredient}".Trim(),
                    DisplayUsed = ing.AmountUsed > 0 ? ing.AmountUsed.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty
                }).ToList()
            }).ToList();

            ItemsProductionList.ItemsSource = _presentationItems;
        }

        //------------------------------------------------------------------------------------------------//

        /// <summary>
        /// Synchronizes user input from UI presentation view models back into the underlying order model.
        /// </summary>
        private void SyncPresentationItemsToOrder()
        {
            // Ensure both presentation items and order production items collections exist
            if (_presentationItems == null || _order.productionItems == null) return;

            // Iterate through each presentation item and update the corresponding order item
            for (int i = 0; i < _presentationItems.Count && i < _order.productionItems.Count; i++)
            {
                var pvm = _presentationItems[i];
                var pItem = _order.productionItems[i];

                // Sync baker/operator notes
                pItem.Notes = pvm.Notes ?? string.Empty;

                // Sync ingredient amounts used if collections are present
                if (pvm.Ingredients != null && pItem.ReqIngredients != null)
                {
                    for (int j = 0; j < pvm.Ingredients.Count && j < pItem.ReqIngredients.Count; j++)
                    {
                        var ivm = pvm.Ingredients[j];
                        var ing = pItem.ReqIngredients[j];

                        // Parse the user-entered amount using invariant culture format
                        if (double.TryParse(ivm.DisplayUsed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedUsed))
                        {
                            ing.AmountUsed = parsedUsed;
                        }
                        // Reset to 0 if the field was left blank or whitespace
                        else if (string.IsNullOrWhiteSpace(ivm.DisplayUsed))
                        {
                            ing.AmountUsed = 0;
                        }
                    }
                }
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Back button click event
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();
            _orderService.SaveOrder(_order);
            NavigationService?.GoBack();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Print button click event
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();
            _orderService.SaveOrder(_order);

            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                MessageBox.Show($"Order sheet for {_order.OrderId} sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler to upload an Excel file with notes and used quantities
        private void BtnUploadExcel_Click(object sender, RoutedEventArgs e)
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
                    bool imported = _excelService.ImportOrderFromExcel(dialog.FileName, _order);
                    if (imported)
                    {
                        // Refresh presentation items and UI bindings
                        PopulateUI();

                        var res = MessageBox.Show(
                            "Production Excel sheet uploaded successfully!\nUsed quantities and notes have been populated.\n\nDo you want to mark this order as Completed now?",
                            "Excel Imported",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (res == MessageBoxResult.Yes)
                        {
                            BtnModalDone_Click(sender, e);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not read valid production data from the selected Excel file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import Excel: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Adriaan - Event handler to email updated order Excel back to admin
        /// <summary>
        /// Synchronizes UI changes, persists the order, exports it to an Excel workbook,
        /// and drafts an email back to the admin via Microsoft Outlook (or fallback default mail client).
        /// </summary>
        private void BtnEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Synchronize latest user input from presentation models back to order model
                SyncPresentationItemsToOrder();

                // Persist updated order to database/storage
                _orderService.SaveOrder(_order);

                // Export updated order data (actuals and notes) to Excel spreadsheet
                string filePath = _excelService.ExportOrderToExcel(_order);

                bool emailSent = false;
                try
                {
                    // Attempt direct email draft creation via Microsoft Outlook COM automation
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType)!;
                        dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
                        mailItem.Subject = $"Completed Production Order {_order.OrderId} - {_order.Customer}";
                        mailItem.Body = $"Hi Admin,\n\nPlease find attached the updated production order Excel sheet for {_order.Customer} (Order {_order.OrderId}) with actual quantities used and baker notes.\n\nKind regards,\nBakery Team";
                        mailItem.Attachments.Add(filePath);
                        mailItem.Display(false); // Display Outlook mail window without modal blocking
                        emailSent = true;
                    }
                }
                catch (Exception comEx)
                {
                    // Log COM interop failure (e.g. if Outlook is not installed or lacks COM permissions)
                    Console.WriteLine($"[Outlook COM] Direct Outlook launch unavailable: {comEx.Message}");
                }

                // Fallback: If Outlook COM automation is unavailable, launch default mail client and file explorer
                if (!emailSent)
                {
                    string subject = Uri.EscapeDataString($"Completed Production Order {_order.OrderId} - {_order.Customer}");
                    string body = Uri.EscapeDataString($"Hi Admin,\n\nProduction order Excel file saved at:\n{filePath}\n\nActual quantities used and notes have been recorded.");
                    
                    try
                    {
                        // Open default system mail client with pre-filled subject and body
                        Process.Start(new ProcessStartInfo { FileName = $"mailto:?subject={subject}&body={body}", UseShellExecute = true });
                    }
                    catch { }

                    // Open Windows Explorer with the exported Excel file highlighted for easy attachment
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{filePath}\"", UseShellExecute = true });
                    MessageBox.Show($"Updated order exported to Excel:\n{filePath}\n\nPlease attach this file to email back to the admin.", "Email Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // Display error prompt if export, save, or general email dispatch encounters a failure
                MessageBox.Show($"Email failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Mark Completed button click event
        private void BtnMarkCompleted_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();
            CompletedModalOverlay.Visibility = Visibility.Visible;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Done button click event in the modal overlay
        private void BtnModalDone_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();

            // Mark status as completed
            _order.Status = "Completed";

            // If any inputs were left at zero, default them upon completion
            foreach (var item in _order.productionItems ?? new List<ProductionItem>())
            {
                if (item.packaging.PansUsed <= 0)
                {
                    item.packaging.PansUsed = item.packaging.Pans;
                }
                if (item.packaging.TrolleysUsed <= 0)
                {
                    item.packaging.TrolleysUsed = item.packaging.Trolleys;
                }
                foreach (var ing in item.ReqIngredients ?? new List<Ingredients>())
                {
                    if (ing.AmountUsed <= 0)
                    {
                        ing.AmountUsed = Math.Round(ing.IngredientAmount + ing.AdditionsAmount, 2);
                    }
                }
            }

            // Persist update through the shared OrderService
            _orderService.SaveOrder(_order);

            // Return to the user orders dashboard
            NavigationService?.Navigate(new UsersOrderPage());
        }
    }

    //------------------------------------------------------------------------------------------------//

    // View models supporting user data-entry
    public class UserProductionItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public Packaging Packaging { get; set; } = new();
        public List<UserIngredientViewModel> Ingredients { get; set; } = new();
        public string Notes { get; set; } = string.Empty;

        private string _pansUsedStr = string.Empty;
        private bool _pansInitialized = false;

        public string PansUsed
        {
            get
            {
                if (!_pansInitialized)
                {
                    _pansInitialized = true;
                    _pansUsedStr = Packaging.PansUsed > 0 ? Packaging.PansUsed.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                }
                return _pansUsedStr;
            }
            set
            {
                _pansInitialized = true;
                _pansUsedStr = value ?? string.Empty;
                if (double.TryParse(_pansUsedStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    Packaging.PansUsed = d;
                }
                else if (string.IsNullOrWhiteSpace(_pansUsedStr))
                {
                    Packaging.PansUsed = 0;
                }
            }
        }

        private string _trolleysUsedStr = string.Empty;
        private bool _trolleysInitialized = false;

        public string TrolleysUsed
        {
            get
            {
                if (!_trolleysInitialized)
                {
                    _trolleysInitialized = true;
                    _trolleysUsedStr = Packaging.TrolleysUsed > 0 ? Packaging.TrolleysUsed.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                }
                return _trolleysUsedStr;
            }
            set
            {
                _trolleysInitialized = true;
                _trolleysUsedStr = value ?? string.Empty;
                if (double.TryParse(_trolleysUsedStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    Packaging.TrolleysUsed = d;
                }
                else if (string.IsNullOrWhiteSpace(_trolleysUsedStr))
                {
                    Packaging.TrolleysUsed = 0;
                }
            }
        }
    }

    //------------------------------------------------------------------------------------------------//

    public class UserIngredientViewModel
    {
        public string IngredientName { get; set; } = string.Empty;
        public string DisplayAmount { get; set; } = string.Empty;
        public string DisplayAdditional { get; set; } = string.Empty;
        public string DisplayUsed { get; set; } = string.Empty;
    }
}

//--------------------------------------End of File----------------------------------------------------------//
