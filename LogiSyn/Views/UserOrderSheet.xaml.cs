// Adriaan
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class UserOrderSheetView : UserControl
    {
        private readonly OrderRow? _order;
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();
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

            // 1. Try local lookup first (instant UI update)
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

        private async Task SaveUserSheetChangesAsync(bool markAsCompleted)
        {
            if (Sheet.DataContext is not SheetData sheetData) return;

            // If we don't have a loaded OrderScaled yet, try to load or create skeleton
            if (_scaledOrder == null && _order != null)
            {
                _scaledOrder = _orderService.GetOrderById(_order.Number);
            }

            if (_scaledOrder == null && _order != null)
            {
                _scaledOrder = new OrderScaled
                {
                    OrderId = _order.Number,
                    Customer = _order.Customer,
                    OrderDate = _order.Date != default ? _order.Date : DateTime.Now,
                    Status = markAsCompleted ? "Completed" : "Pending",
                    ProductionItems = sheetData.Products?.Select(p => new ProductionItem
                    {
                        ProductName = $"{p.Amount} {p.Name}".Trim(),
                        ProductionLine = p.Production ?? "Production 1",
                        Notes = p.Notes ?? "",
                        Packaging = new Packaging
                        {
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
                    }).ToList() ?? new()
                };
            }

            if (_scaledOrder == null) return;

            // Apply sheet values (Used ingredients, Used pans/trolleys, Notes) to _scaledOrder
            SheetData.ApplyToOrderScaled(sheetData, _scaledOrder, isCompleting: markAsCompleted);

            if (markAsCompleted)
            {
                _scaledOrder.Status = "Completed";
                if (_order != null) _order.Status = "Complete";

                // Re-render UI in completed state
                PopulateUI(_scaledOrder);
            }

            // Persist order to database (memory, Data/orders.json, MongoDB, SQL Server)
            _orderService.SaveOrder(_scaledOrder);

            // Sync with backend API
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
                MessageBox.Show($"Sheet for {_order?.Number ?? _scaledOrder?.OrderId} sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                ShellWindow.Current?.Navigate("orders");
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