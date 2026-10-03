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
        /// <summary>
        /// Retrieves all orders from the repository
        /// </summary>
        Task<IEnumerable<OrderScaled>> GetAllOrdersAsync();

        /// <summary>
        /// Retrieves a specific order by its ID
        /// </summary>
        Task<OrderScaled?> GetOrderByIdAsync(string orderId);

        /// <summary>
        /// Saves or updates an order in the repository
        /// </summary>
        Task<OrderScaled> SaveOrderAsync(OrderScaled order);

        /// <summary>
        /// Deletes an order from the repository
        /// </summary>
        Task<bool> DeleteOrderAsync(string orderId);

        /// <summary>
        /// Updates the status of an order
        /// </summary>
        Task<bool> UpdateOrderStatusAsync(string orderId, string status);

        /// <summary>
        /// Retrieves all completed orders
        /// </summary>
        Task<IEnumerable<OrderScaled>> GetCompletedOrdersAsync();
    }
}
