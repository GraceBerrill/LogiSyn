using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Model;

namespace LogiSyn.ViewModels
{
    // ViewModel for order ingestion and parsing, supporting Excel, CSV, and PDF order formats
    // with cloud API parsing and offline local parser failover.
    public class CreateOrderViewModel : ViewModelBase
    {
        private readonly OrderService _orderService;
        private readonly ExcelOrderService _excelService;
        private readonly ApiClient _apiClient;

        private string? _filePath;
        private string _fileName = string.Empty;
        private string _dateText = DateTime.Now.ToString("dd MMMM yyyy");
        private bool _isProcessing;
        private string? _errorMessage;

        public CreateOrderViewModel(
            OrderService? orderService = null,
            ExcelOrderService? excelService = null,
            ApiClient? apiClient = null)
        {
            _orderService = orderService ?? App.ServiceProvider?.GetService<OrderService>() ?? new OrderService();
            _excelService = excelService ?? App.ServiceProvider?.GetService<ExcelOrderService>() ?? new ExcelOrderService();
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
        }

        public string? FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    FileName = !string.IsNullOrWhiteSpace(value) ? Path.GetFileName(value) : string.Empty;
                }
            }
        }

        public string FileName
        {
            get => _fileName;
            private set => SetProperty(ref _fileName, value);
        }

        public string DateText
        {
            get => _dateText;
            set => SetProperty(ref _dateText, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Sets the selected order file.
        public void SetSelectedFile(string path)
        {
            FilePath = path;
            ErrorMessage = null;
        }

        // Reads and scales an order from the selected file.
        public async Task<OrderScaled?> ParseSelectedFileAsync()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                ErrorMessage = "Please choose an order file first.";
                return null;
            }

            IsProcessing = true;
            ErrorMessage = null;

            try
            {
                string ext = Path.GetExtension(FilePath).ToLowerInvariant();
                OrderScaled? order = null;

                if (ext == ".xlsx" || ext == ".xls" || ext == ".csv")
                {
                    order = await Task.Run(() => _excelService.ReadOrderFromExcel(FilePath));
                }
                else if (ext == ".pdf")
                {
                    try
                    {
                        order = await _apiClient.ParsePdfOrderAsync(FilePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CreateOrderViewModel] Cloud API PDF parse failed ({ex.Message}). Engaging local offline parser.");
                    }

                    if (order == null)
                    {
                        order = await Task.Run(() => _orderService.ReadAndScaleOrder(FilePath));
                    }
                }
                else
                {
                    ErrorMessage = "Unsupported file type. Please select an Excel (.xlsx, .xls, .csv) or PDF (.pdf) file.";
                    return null;
                }

                if (order == null)
                {
                    ErrorMessage = "Unable to parse the order file. Please verify that the selected file is a valid order file.";
                }

                return order;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while creating the order: {ex.Message}";
                return null;
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
