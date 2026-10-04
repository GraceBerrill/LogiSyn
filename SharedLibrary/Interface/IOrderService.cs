using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Interface
{
    public interface IOrderService
    {
        OrderScaled ReadAndScaleOrder(string pdfPath, string fallbackOrderId = "#001");
        void RecalRawMaterials(OrderScaled order);

        // Synchronous methods (for backwards compatibility)
        IEnumerable<OrderScaled> GetOrders();
        IEnumerable<OrderScaled> GetHistory();
        OrderScaled? GetOrderById(string orderId);
        void SaveOrder(OrderScaled order);
        void CompleteOrder(string orderId, Action<OrderScaled>? recordOrderData = null);
        EmailMessageModel BuildScalingSheetEmail(OrderScaled order);

        // Async methods
        Task<IEnumerable<OrderScaled>> GetOrdersAsync(bool forceRefresh = false);
        Task<OrderScaled?> GetOrderByIdAsync(string orderId);
        Task<OrderScaled> SaveOrderAsync(OrderScaled order);
        Task CompleteOrderAsync(string orderId, Action<OrderScaled>? recordOrderData = null);
    }
}
