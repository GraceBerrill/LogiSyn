// Adriaan
using LogiSyn.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogiSyn.Interface
{
    /// <summary>
    /// Interface for order persistence operations across different data stores
    /// </summary>
    public interface IOrderRepository
    {
        Task<IEnumerable<OrderScaled>> GetAllOrdersAsync();
        Task<OrderScaled?> GetOrderByIdAsync(string orderId);
        Task<OrderScaled> SaveOrderAsync(OrderScaled order);
        Task<bool> DeleteOrderAsync(string orderId);
        Task<bool> UpdateOrderStatusAsync(string orderId, string status);
        Task<IEnumerable<OrderScaled>> GetCompletedOrdersAsync();
    }
}

//--------------------------------------End of File----------------------------------------------------------//
