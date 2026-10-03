using LogiSyn.Interface;
using SharedLibrary.Model;
using LogiSyn.Services;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for AdminOrderHistory.xaml
    /// </summary>
    public partial class AdminOrderHistory : Page
    {
        private readonly IOrderService _orderService;
        private List<OrderScaled> _completedOrders = new();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the AdminOrderHistory class
        public AdminOrderHistory()
        {
            InitializeComponent();

            _orderService = new OrderService();
            TxtCurrentDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
            this.Loaded += (s, e) => LoadHistory();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to load the order history from the order service
        private void LoadHistory()
        {
            var history = _orderService.GetHistory().ToList();

            // If there are no orders in the history, create mock data for testing
            // TODO : Remove this mock data creation in production
            if (!history.Any())
            {
                var mock1 = new OrderScaled
                {
                    OrderId = "#001",
                    Customer = "Checkers",
                    OrderDate = new DateTime(2026, 5, 9),
                    Status = "Completed",
                    productionItems = new List<ProductionItem>
                    {
                        new ProductionItem
                        {
                            ProductName = "150 Sandwich Bread",
                            ProductionLine = "Production 2",
                            packaging = new Packaging { Pans = 4, Trolleys = 1 },
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Flour", IngredientAmount = 5, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Yeast", IngredientAmount = 1, MeasuredIngredient = "kg" }
                            }
                        }
                    }
                };

                var mock2 = new OrderScaled
                {
                    OrderId = "#002",
                    Customer = "Spar",
                    OrderDate = new DateTime(2026, 5, 9),
                    Status = "Completed",
                    productionItems = new List<ProductionItem>
                    {
                        new ProductionItem
                        {
                            ProductName = "250 Hamburger Rolls",
                            ProductionLine = "Production 1",
                            packaging = new Packaging { Pans = 2, Trolleys = 1 },
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Flour", IngredientAmount = 6, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Eggs", IngredientAmount = 27, MeasuredIngredient = "dozen" },
                                new Ingredients { IngredientName = "Salt", IngredientAmount = 3, MeasuredIngredient = "bags" }
                            }
                        },
                        new ProductionItem
                        {
                            ProductName = "100 Rolls",
                            ProductionLine = "Production 1",
                            packaging = new Packaging { Pans = 2, Trolleys = 1 },
                            ReqIngredients = new List<Ingredients>
                            {
                                new Ingredients { IngredientName = "Flour", IngredientAmount = 6, MeasuredIngredient = "bags" },
                                new Ingredients { IngredientName = "Eggs", IngredientAmount = 27, MeasuredIngredient = "dozen" },
                                new Ingredients { IngredientName = "Salt", IngredientAmount = 3, MeasuredIngredient = "bags" }
                            }
                        }
                    }
                };

                _orderService.SaveOrder(mock1);
                _orderService.SaveOrder(mock2);
                history = _orderService.GetHistory().ToList();
            }

            _completedOrders = history;
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to apply filters based on search text and date sort selection
        private void ApplyFilters()
        {
            // Defensive guard: HistoryDataGrid may be null during InitializeComponent if events fire early
            if (HistoryDataGrid == null)
                return;

            // Start with the full list of completed orders
            var filtered = _completedOrders.AsEnumerable();

            // Apply search filter based on the text in the search box
            string search = TxtSearchBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(o =>
                    (!string.IsNullOrEmpty(o.OrderId) && o.OrderId.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(o.Customer) && o.Customer.ToLower().Contains(search)));
            }

            // Apply date sort filter based on the selected item in the date sort combo box
            if (CmbDateSort?.SelectedItem is ComboBoxItem selectedItem)
            {
                string sort = selectedItem.Content?.ToString() ?? "";
                if (sort == "Newest First")
                    filtered = filtered.OrderByDescending(o => o.OrderDate);
                else if (sort == "Oldest First")
                    filtered = filtered.OrderBy(o => o.OrderDate);
            }

            HistoryDataGrid.ItemsSource = null;
            HistoryDataGrid.ItemsSource = filtered.ToList();
        }


        //------------------------------------------------------------------------------------------------//

        // Event handler for when the text in the search box changes
        private void TxtSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for when the selection in the date sort combo box changes
        private void CmbDateSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the "View" button click event in the order history data grid
        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderScaled selectedOrder)
            {
                NavigationService?.Navigate(new AdminOrderBreakdown(selectedOrder));
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
