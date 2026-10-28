using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Repositories;
using Microsoft.Extensions.Logging;
using SharedLibrary.Interface;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    // Order management service orchestrating PDF ingestion, recipe scaling, persistence, and status tracking
    public class OrderService : IOrderService
    {
        private readonly IProductService _productService;
        private readonly MongoOrderRepository? _mongoRepository;
        private readonly SqlOrderRepository? _sqlRepository;
        private readonly ITempRecipeService? _recipeService;
        private readonly ILogger<OrderService>? _logger;
        private readonly OrderScalingService _scalingService;
        private readonly PdfOrderParserService _pdfParser;
        private static readonly OrderLocalStorage _storage = new();

        public OrderService()
            : this(new ProductService(), OrderRepositoryFactory.CreateDefaultMongoRepository(), OrderRepositoryFactory.CreateDefaultSqlRepository(), new TempRecipeService()) { }

        public OrderService(IProductService productService)
            : this(productService, OrderRepositoryFactory.CreateDefaultMongoRepository(), OrderRepositoryFactory.CreateDefaultSqlRepository(), new TempRecipeService()) { }

        public OrderService(MongoOrderRepository? mongoRepository, SqlOrderRepository? sqlRepository)
            : this(new ProductService(), mongoRepository, sqlRepository, new TempRecipeService()) { }

        public OrderService(IProductService productService, MongoOrderRepository? mongoRepository, SqlOrderRepository? sqlRepository)
            : this(productService, mongoRepository, sqlRepository, new TempRecipeService()) { }

        public OrderService(
            IProductService productService,
            MongoOrderRepository? mongoRepository,
            SqlOrderRepository? sqlRepository,
            ITempRecipeService? recipeService,
            ILogger<OrderService>? logger = null)
        {
            _productService = productService ?? new ProductService();
            _mongoRepository = mongoRepository;
            _sqlRepository = sqlRepository;
            _recipeService = recipeService ?? new TempRecipeService();
            _logger = logger;
            _scalingService = new OrderScalingService();
            _pdfParser = new PdfOrderParserService();

            _storage.EnsureLoaded();
        }

        // Parses the PDF, extracts order details, and scales the ingredients based on recipes
        public OrderScaled ReadAndScaleOrder(string pdfPath, string fallbackOrderId = "#001")
        {
            return _pdfParser.ReadAndScaleOrder(
                pdfPath,
                fallbackOrderId,
                _productService,
                _recipeService,
                EnsureUniqueOrderId,
                RecalRawMaterials);
        }

        // Ensures that the generated order ID is unique by checking against existing orders
        public string EnsureUniqueOrderId(string baseOrderId)
        {
            if (string.IsNullOrWhiteSpace(baseOrderId))
                baseOrderId = "#001";

            string candidateId = baseOrderId;
            var existingOrderIds = new HashSet<string>(GetOrders().Select(o => o.OrderId), StringComparer.OrdinalIgnoreCase);

            var numMatch = Regex.Match(baseOrderId, @"^(#?)(\d+)$");
            if (numMatch.Success)
            {
                string prefix = numMatch.Groups[1].Value;
                string digits = numMatch.Groups[2].Value;
                int num = int.Parse(digits);
                int padLength = digits.Length;

                while (existingOrderIds.Contains(candidateId))
                {
                    num++;
                    candidateId = $"{prefix}{num.ToString().PadLeft(padLength, '0')}";
                }

                return candidateId;
            }

            int suffix = 1;
            while (existingOrderIds.Contains(candidateId))
            {
                candidateId = $"{baseOrderId}-{suffix}";
                suffix++;
            }

            return candidateId;
        }

        // Recalculates total raw materials required for an order via OrderScalingService
        public void RecalRawMaterials(OrderScaled order) => _scalingService.RecalRawMaterials(order);

        // Invalidates the in-memory cache
        public static void InvalidateCache() => _storage.InvalidateCache();

        void IOrderService.InvalidateCache() => InvalidateCache();

        // Asynchronously fetches orders from MongoDB, falling back to SQL Server, then local memory
        public async Task<IEnumerable<OrderScaled>> GetOrdersAsync(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                InvalidateCache();
            }

            if (_mongoRepository != null)
            {
                try
                {
                    var mongoOrders = (await _mongoRepository.GetAllOrdersAsync()).ToList();
                    if (mongoOrders.Count > 0)
                    {
                        _storage.MergeOrders(mongoOrders);
                        _storage.SaveLocalOrders();
                        return _storage.GetSnapshot();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error fetching orders from MongoDB");
                }
            }

            if (_sqlRepository != null)
            {
                try
                {
                    var sqlOrders = (await _sqlRepository.GetAllOrdersAsync()).ToList();
                    if (sqlOrders.Count > 0)
                    {
                        _storage.MergeOrders(sqlOrders);
                        _storage.SaveLocalOrders();
                        return _storage.GetSnapshot();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error fetching orders from SQL");
                }
            }

            return _storage.GetSnapshot();
        }

        // Synchronous retrieval wrapper
        public IEnumerable<OrderScaled> GetOrders()
        {
            try
            {
                return Task.Run(async () => await GetOrdersAsync(forceRefresh: false)).GetAwaiter().GetResult();
            }
            catch
            {
                return _storage.GetSnapshot();
            }
        }

        // Retrieves completed orders for history
        public IEnumerable<OrderScaled> GetHistory()
        {
            return GetOrders().Where(o =>
                string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Asynchronously retrieves an order by ID (with resilient # prefix matching)
        public async Task<OrderScaled?> GetOrderByIdAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return null;

            string cleanTarget = orderId.Trim().TrimStart('#');
            var all = await GetOrdersAsync(forceRefresh: false);
            return all.FirstOrDefault(o =>
                o.OrderId.Equals(orderId, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(cleanTarget) && o.OrderId.Trim().TrimStart('#').Equals(cleanTarget, StringComparison.OrdinalIgnoreCase)));
        }

        // Synchronous retrieval by ID
        public OrderScaled? GetOrderById(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return null;

            string cleanTarget = orderId.Trim().TrimStart('#');
            return GetOrders().FirstOrDefault(o =>
                o.OrderId.Equals(orderId, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(cleanTarget) && o.OrderId.Trim().TrimStart('#').Equals(cleanTarget, StringComparison.OrdinalIgnoreCase)));
        }

        // Asynchronously saves or updates an order locally and persists across MongoDB and SQL Server
        public async Task<OrderScaled> SaveOrderAsync(OrderScaled order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            _storage.UpsertOrder(order);
            _storage.SaveLocalOrders();

            if (_mongoRepository != null)
            {
                try
                {
                    await _mongoRepository.SaveOrderAsync(order);
                    _logger?.LogInformation("Successfully saved order {OrderId} to MongoDB", order.OrderId);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error saving order to MongoDB for {OrderId}", order.OrderId);
                }
            }

            if (_sqlRepository != null)
            {
                try
                {
                    await _sqlRepository.SaveOrderAsync(order);
                    _logger?.LogInformation("Successfully saved order {OrderId} to SQL", order.OrderId);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error saving order to SQL for {OrderId}", order.OrderId);
                }
            }

            return order;
        }

        // Synchronous save wrapper
        public void SaveOrder(OrderScaled order)
        {
            if (order == null) return;
            try
            {
                Task.Run(async () => await SaveOrderAsync(order)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in sync SaveOrder");
            }
        }

        // Completes an order asynchronously
        public async Task CompleteOrderAsync(string orderId, Action<OrderScaled>? recordOrderData = null)
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null) return;

            recordOrderData?.Invoke(order);
            order.Status = "Completed";
            await SaveOrderAsync(order);
        }

        public async Task CompleteOrderAsync(string orderId, OrderScaled completedOrder)
        {
            if (completedOrder == null) throw new ArgumentNullException(nameof(completedOrder));
            completedOrder.OrderId = orderId;
            completedOrder.Status = "Completed";
            await SaveOrderAsync(completedOrder);
        }

        // Synchronous complete wrapper
        public void CompleteOrder(string orderId, Action<OrderScaled>? recordOrderData = null)
        {
            var order = GetOrderById(orderId);
            if (order == null) return;

            recordOrderData?.Invoke(order);
            order.Status = "Completed";
            SaveOrder(order);
        }

        public void CompleteOrder(string orderId, OrderScaled completedOrder)
        {
            if (completedOrder == null) return;
            completedOrder.OrderId = orderId;
            completedOrder.Status = "Completed";
            SaveOrder(completedOrder);
        }

        // Email template for order scaling sheet
        public EmailMessageModel BuildScalingSheetEmail(OrderScaled order) => _scalingService.BuildScalingSheetEmail(order);
    }
}
