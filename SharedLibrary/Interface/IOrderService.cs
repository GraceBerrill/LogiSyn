using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLibrary.Interface
{
    public interface IOrderService
    {
        OrderScaled ReadAndScaleOrder(string pdfPath, string fallbackOrderId = "#001");
        void RecalRawMaterials(OrderScaled order);

        IEnumerable<OrderScaled> GetOrders();
        IEnumerable<OrderScaled> GetHistory();
        OrderScaled? GetOrderById(string orderId);
        void SaveOrder(OrderScaled order);
        void CompleteOrder(string orderId, Action<OrderScaled> recordOrderData);
    }
}
