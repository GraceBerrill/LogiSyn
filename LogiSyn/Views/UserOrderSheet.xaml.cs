// Adriaan
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;

namespace LogiSyn.Views
{
    public partial class UserOrderSheetView : UserControl
    {
        private readonly OrderRow? _order;
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();
        private readonly ExcelOrderService _excelService = new ExcelOrderService();
        private OrderScaled? _scaledOrder;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the UserOrderSheetView class
        public UserOrderSheetView(OrderRow order)
        {
            InitializeComponent();
            _order = order;

            // Load the real order data and populate the production sheet
            LoadOrderData();
        }

        //------------------------------------------------------------------------------------------------//

        private void LoadOrderData()
        {
            if (_order == null) return;

            // Look for order on the local db
            _scaledOrder = _orderService.GetOrderById(_order.Number);

            if (_scaledOrder != null)
            {
                PopulateUI(_scaledOrder);
            }
            else
            {
                // Fallback while loading
                bool done = _order.IsComplete;
                var data = SampleData.Sheet(done);
                data.Title = $"{_order.Customer} Order {_order.Number}".Trim();
                Sheet.IsReadOnly = done;
                Sheet.DataContext = data;
                CompleteButton.Visibility = done ? Visibility.Collapsed : Visibility.Visible;

                // Try fetching from API asynchronously
                FetchOrderFromApiAsync();
            }
        }

        private async void FetchOrderFromApiAsync()
        {
            if (_order == null) return;

            try
            {
                var apiOrder = await _apiClient.GetOrderByIdAsync(_order.Number);
                if (apiOrder != null)
                {
                    _scaledOrder = apiOrder;
                    PopulateUI(_scaledOrder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserOrderSheet] Error fetching order from API: {ex.Message}");
            }
        }

        private void PopulateUI(OrderScaled scaled)
        {
            var data = SheetData.FromOrderScaled(scaled);
            bool done = string.Equals(scaled.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(scaled.Status, "Complete", StringComparison.OrdinalIgnoreCase);

            Sheet.IsReadOnly = done;
            Sheet.DataContext = data;
            CompleteButton.Visibility = done ? Visibility.Collapsed : Visibility.Visible;
        }

        //------------------------------------------------------------------------------------------------//

        // Saves changes made to the user production sheet, including actual quantities used, packaging, and notes.
        private async Task SaveUserSheetChangesAsync(bool markAsCompleted)
        {
            // Ensure sheet DataContext contains a valid SheetData instance
            if (Sheet.DataContext is not SheetData sheetData) return;

            // If we don't have a loaded OrderScaled yet, try to load it from storage
            if (_scaledOrder == null && _order != null)
            {
                _scaledOrder = _orderService.GetOrderById(_order.Number);
            }

            // If still not found, construct a new skeleton OrderScaled model from sheet products
            if (_scaledOrder == null && _order != null)
            {
                _scaledOrder = new OrderScaled
                {
                    OrderId = _order.Number,
                    Customer = _order.Customer,
                    OrderDate = _order.Date != default ? _order.Date : DateTime.Now,
                    Status = markAsCompleted ? "Completed" : "Pending",
                    ProductionItems = sheetData.Products?.Select(p =>
                    {
                        int parsedAmount = int.TryParse(p.Amount, out int a) ? a : 100;
                        string cleanName = p.Name ?? string.Empty;
                        var match = System.Text.RegularExpressions.Regex.Match(cleanName, @"^(\d+)\s+(.+)$");
                        if (match.Success)
                        {
                            if (parsedAmount == 100 && int.TryParse(match.Groups[1].Value, out int embeddedAmt))
                            {
                                parsedAmount = embeddedAmt;
                            }
                            cleanName = match.Groups[2].Value.Trim();
                        }

                        return new ProductionItem
                        {
                            ProductName = cleanName,
                            Amount = parsedAmount,
                            ProductionLine = p.Production ?? "Production 1",
                            Notes = p.Notes ?? "",
                            Packaging = new Packaging
                            {
                                // Safely parse pans and trolleys counts using invariant culture, with fallback defaults
                                Pans = double.TryParse(p.Packaging?.FirstOrDefault(x => x.Name == "Pans")?.Amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pans) ? pans : 4,
                                Trolleys = double.TryParse(p.Packaging?.FirstOrDefault(x => x.Name == "Trolleys")?.Amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double tr) ? tr : 2
                            },
                            ReqIngredients = p.Ingredients?.Select(si => new Ingredients
                            {
                                IngredientName = si.Name,
                                IngredientAmount = 10,
                                AdditionsAmount = 1,
                                MeasuredIngredient = "kg"
                            }).ToList() ?? new()
                        };
                    }).ToList() ?? new()
                };
            }

            // Abort if order initialization could not be completed
            if (_scaledOrder == null) return;

            // Apply sheet values (Used ingredients, Used pans/trolleys, Notes) to _scaledOrder
            SheetData.ApplyToOrderScaled(sheetData, _scaledOrder, isCompleting: markAsCompleted);

            // Handle completion state transitions and UI updates
            if (markAsCompleted)
            {
                _scaledOrder.Status = "Completed";
                if (_order != null) _order.Status = "Complete";

                // Re-render UI into completed/read-only state
                PopulateUI(_scaledOrder);
            }

            // Persist order to local storage/database
            try
            {
                await _orderService.SaveOrderAsync(_scaledOrder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserOrderSheet] Local save failed: {ex.Message}");
                // fallback to synchronous save if async failed for some reason
                try { _orderService.SaveOrder(_scaledOrder); } catch { }
            }

            // Asynchronously sync order and status with the backend REST API
            try
            {
                await _apiClient.SaveOrderAsync(_scaledOrder);
                if (markAsCompleted)
                {
                    await _apiClient.UpdateOrderStatusAsync(_scaledOrder.OrderId, "Completed");
                }
            }
            catch (Exception ex)
            {
                // Catch and log API communication errors without interrupting the user workflow
                Console.WriteLine($"[UserOrderSheet] Error syncing with API: {ex.Message}");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Back button click event
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Sheet.IsReadOnly)
            {
                await SaveUserSheetChangesAsync(markAsCompleted: false);
            }
            ShellWindow.Current?.Navigate("orders");
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Print button click event
        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Sheet.IsReadOnly)
            {
                await SaveUserSheetChangesAsync(markAsCompleted: false);
            }

            // Create a PrintDialog and show it to the user.
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                printDlg.PrintVisual(Sheet, $"Sheet for {_order?.Number ?? _scaledOrder?.OrderId}");
                MessageBox.Show($"Sheet for {_order?.Number ?? _scaledOrder?.OrderId} sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                ShellWindow.Current?.Navigate("orders");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler to email updated order Excel back to admin
        private async void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Sheet.IsReadOnly)
            {
                await SaveUserSheetChangesAsync(markAsCompleted: false);
            }

            if (_scaledOrder == null && _order != null)
            {
                _scaledOrder = _orderService.GetOrderById(_order.Number);
            }

            if (_scaledOrder == null)
            {
                MessageBox.Show("No active order available to email.", "Email Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string filePath = _excelService.ExportOrderToExcel(_scaledOrder);

                bool emailSent = false;
                try
                {
                    Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType)!;
                        dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
                        mailItem.Subject = $"Completed Production Order {_scaledOrder.OrderId} - {_scaledOrder.Customer}";
                        mailItem.Body = $"Hi Admin,\n\nPlease find attached the updated production order Excel sheet for {_scaledOrder.Customer} (Order {_scaledOrder.OrderId}) with actual quantities used and baker notes.\n\nKind regards,\nBakery Team";
                        mailItem.Attachments.Add(filePath);
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
                    string subject = Uri.EscapeDataString($"Completed Production Order {_scaledOrder.OrderId} - {_scaledOrder.Customer}");
                    string body = Uri.EscapeDataString($"Hi Admin,\n\nProduction order Excel file saved at:\n{filePath}\n\nActual quantities used and notes have been recorded.");
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = $"mailto:?subject={subject}&body={body}", UseShellExecute = true });
                    }
                    catch { }

                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{filePath}\"", UseShellExecute = true });
                    MessageBox.Show($"Updated order exported to Excel:\n{filePath}\n\nPlease attach this file to email back to the admin.", "Email Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Email failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler to upload an Excel file with notes and used quantities
        private async void UploadExcelButton_Click(object sender, RoutedEventArgs e)
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
                    if (_scaledOrder == null && _order != null)
                    {
                        _scaledOrder = _orderService.GetOrderById(_order.Number) 
                            ?? new OrderScaled { OrderId = _order.Number, Customer = _order.Customer };
                    }

                    bool imported = false;
                    if (_scaledOrder != null)
                    {
                        imported = _excelService.ImportOrderFromExcel(dialog.FileName, _scaledOrder);
                    }
                    else
                    {
                        _scaledOrder = _excelService.ReadOrderFromExcel(dialog.FileName);
                        imported = _scaledOrder != null;
                    }

                    if (imported && _scaledOrder != null)
                    {
                        // Update UI immediately with the imported used quantities and notes
                        PopulateUI(_scaledOrder);

                        var res = MessageBox.Show(
                            "Production Excel sheet imported successfully!\nUsed quantities and notes have been populated.\n\nDo you want to mark this order as Completed now?",
                            "Excel Imported",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (res == MessageBoxResult.Yes)
                        {
                            await SaveUserSheetChangesAsync(markAsCompleted: true);
                            ShellWindow.Current?.ShowModal(new OrderCompletedModal(), (Brush)FindResource("ScrimDone"), false);
                        }
                        else
                        {
                            await SaveUserSheetChangesAsync(markAsCompleted: false);
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

        // Event handler for the Complete button click event
        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveUserSheetChangesAsync(markAsCompleted: true);

            // popup window with checkmark to show completion
            ShellWindow.Current?.ShowModal(new OrderCompletedModal(), (Brush)FindResource("ScrimDone"), false);
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//