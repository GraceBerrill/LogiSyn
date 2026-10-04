// Adriaan
using System;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class OrderBreakdownView : UserControl
    {
        private readonly OrderService _orderService = new OrderService();
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly AppRole _role;
        private readonly OrderRow? _order;

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
            var scaledOrder = _orderService.GetOrderById(orderId);

            if (scaledOrder != null)
            {
                RenderOrder(scaledOrder);
            }
            else
            {
                string title = _order == null ? "Spar Order #002" : $"{_order.Customer} Order {_order.Number}".Trim();
                if (_role == AppRole.Manager)
                {
                    var data = SampleData.Sheet(true);
                    data.Title = title;
                    PaperHost.Content = new SheetPaper { IsReadOnly = true, DataContext = data };
                }
                else
                {
                    var data = SampleData.Summary(true);
                    data.Title = title;
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
                    RenderOrder(apiOrder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrderBreakdown] Error fetching order from API: {ex.Message}");
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
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Done button click event
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current?.Navigate("history");
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//