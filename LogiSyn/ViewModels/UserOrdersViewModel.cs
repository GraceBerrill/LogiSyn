using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Model;

namespace LogiSyn.ViewModels
{
    // ViewModel for standard user/baker order view, handling order lists and production Excel uploads.
    public class UserOrdersViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly OrderService _orderService;
        private readonly ExcelOrderService _excelService;

        private ObservableCollection<OrderRow> _orders = new();
        private string _dateText = DateTime.Now.ToString("dd MMMM yyyy");
        private bool _isLoading;

        public UserOrdersViewModel(
            ApiClient? apiClient = null,
            OrderService? orderService = null,
            ExcelOrderService? excelService = null)
        {
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
            _orderService = orderService ?? App.ServiceProvider?.GetService<OrderService>() ?? new OrderService();
            _excelService = excelService ?? App.ServiceProvider?.GetService<ExcelOrderService>() ?? new ExcelOrderService();
        }

        public ObservableCollection<OrderRow> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public string DateText
        {
            get => _dateText;
            set => SetProperty(ref _dateText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Loads active orders for baker view with cloud API and local failover.
        public async Task LoadOrdersAsync()
        {
            IsLoading = true;
            try
            {
                List<OrderScaled>? orders = null;
                try
                {
                    orders = await _apiClient.GetOrdersAsync();
                }
                catch { }

                if (orders == null || orders.Count == 0)
                {
                    try
                    {
                        var localOrders = await _orderService.GetOrdersAsync();
                        orders = localOrders?.ToList() ?? new List<OrderScaled>();
                    }
                    catch
                    {
                        orders = new List<OrderScaled>();
                    }
                }

                var rows = orders.Select(o => OrderRow.FromOrderScaled(o)).ToList();
                Orders = new ObservableCollection<OrderRow>(rows);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Ingests and persists an updated production Excel sheet uploaded from the factory floor.
        public async Task<OrderScaled?> ProcessUploadedExcelAsync(string filePath)
        {
            var parsed = await Task.Run(() => _excelService.ReadOrderFromExcel(filePath));
            if (parsed != null)
            {
                try { await _orderService.SaveOrderAsync(parsed); } catch { }
                try { await _apiClient.SaveOrderAsync(parsed); } catch { }
                await LoadOrdersAsync();
            }
            return parsed;
        }
    }
}
