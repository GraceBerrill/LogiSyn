using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Interface;
using SharedLibrary.Model;

namespace LogiSyn.ViewModels
{
    // ViewModel for OrderSheetsView, handling scaled recipe viewing, ingredient recalculation,
    // persistence across cloud and local stores, and Excel export.
    public class OrderSheetsViewModel : ViewModelBase
    {
        private readonly IOrderService _orderService;
        private readonly ApiClient _apiClient;
        private readonly ExcelOrderService _excelService;

        private OrderScaled _currentOrder;
        private string _orderTitle = string.Empty;
        private string _orderDateText = string.Empty;
        private ObservableCollection<ProductionItemDisplayModel> _displayItems = new();
        private ObservableCollection<ScalingItemDisplayModel> _displayScaling = new();
        private bool _isSaving;

        public OrderSheetsViewModel(
            OrderScaled? order = null,
            IOrderService? orderService = null,
            ApiClient? apiClient = null,
            ExcelOrderService? excelService = null)
        {
            _currentOrder = order ?? new OrderScaled();
            _orderService = orderService ?? App.ServiceProvider?.GetService<OrderService>() ?? new OrderService();
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
            _excelService = excelService ?? App.ServiceProvider?.GetService<ExcelOrderService>() ?? new ExcelOrderService();

            Populate();
        }

        public OrderScaled CurrentOrder
        {
            get => _currentOrder;
            set
            {
                if (SetProperty(ref _currentOrder, value))
                {
                    Populate();
                }
            }
        }

        public string OrderTitle
        {
            get => _orderTitle;
            private set => SetProperty(ref _orderTitle, value);
        }

        public string OrderDateText
        {
            get => _orderDateText;
            private set => SetProperty(ref _orderDateText, value);
        }

        public ObservableCollection<ProductionItemDisplayModel> DisplayItems
        {
            get => _displayItems;
            private set => SetProperty(ref _displayItems, value);
        }

        public ObservableCollection<ScalingItemDisplayModel> DisplayScaling
        {
            get => _displayScaling;
            private set => SetProperty(ref _displayScaling, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        // Populates display collections and formatted header strings from the current scaled order.
        public void Populate()
        {
            OrderTitle = $"{_currentOrder.Customer} Order {_currentOrder.OrderId}".Trim();
            OrderDateText = _currentOrder.OrderDate.ToString("d MMMM yyyy");

            var items = _currentOrder.productionItems.Select(item => new ProductionItemDisplayModel
            {
                ProductName = item.Amount > 0 && !(item.ProductName ?? string.Empty).StartsWith($"{item.Amount} ")
                    ? $"{item.Amount} {item.ProductName}"
                    : (item.ProductName ?? string.Empty),
                ProductionLine = item.ProductionLine,
                Pans = item.packaging.Pans,
                Trolleys = item.packaging.Trolleys,
                IngredientsSummary = string.Join(",  ", item.ReqIngredients.Select(i =>
                    $"{i.IngredientName}: {i.IngredientAmount + i.AdditionsAmount} {i.MeasuredIngredient}"))
            }).ToList();

            DisplayItems = new ObservableCollection<ProductionItemDisplayModel>(items);

            var rawItems = _currentOrder.RawMaterials.Select(kvp => new ScalingItemDisplayModel
            {
                Key = kvp.Key,
                DisplayValue = $"{kvp.Value.Amount} {kvp.Value.Unit}"
            }).ToList();

            DisplayScaling = new ObservableCollection<ScalingItemDisplayModel>(rawItems);
        }

        // Adds a newly created production item, recalculates ingredients, and refreshes UI.
        public async Task AddItemAsync(ProductionItem item)
        {
            _currentOrder.productionItems.Add(item);
            _orderService.RecalRawMaterials(_currentOrder);
            Populate();
            await SaveOrderAsync();
        }

        // Persists the order locally and to the cloud API.
        public async Task SaveOrderAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentOrder.OrderId))
                return;

            IsSaving = true;
            try
            {
                try
                {
                    await _orderService.SaveOrderAsync(_currentOrder);
                }
                catch { }

                try
                {
                    await _apiClient.SaveOrderAsync(_currentOrder);
                }
                catch { }
            }
            finally
            {
                IsSaving = false;
            }
        }

        // Exports the scaled order to Excel.
        public string ExportOrderToExcel(string? specificPath = null)
        {
            return string.IsNullOrWhiteSpace(specificPath)
                ? _excelService.ExportOrderToExcel(_currentOrder)
                : _excelService.ExportOrderToExcel(_currentOrder, specificPath);
        }
    }

    // Lightweight display model for production items used in OrderSheetsView.
    public class ProductionItemDisplayModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public double Pans { get; set; }
        public double Trolleys { get; set; }
        public string IngredientsSummary { get; set; } = string.Empty;
    }

    // Lightweight display model for raw materials used in OrderSheetsView.
    public class ScalingItemDisplayModel
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
    }
}
