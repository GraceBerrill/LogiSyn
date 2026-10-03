using LogiSyn.Interface;
using LogiSyn.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndersonsBakeryAPI.Repositories
{
    /// <summary>
    /// SQL Server implementation of IOrderRepository using Entity Framework Core
    /// </summary>
    public class SqlOrderRepository : IOrderRepository
    {
        private readonly Data.LogiSynDbContext _context;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the SqlOrderRepository class, which takes a LogiSynDbContext as a dependency.
        public SqlOrderRepository(Data.LogiSynDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        //------------------------------------------------------------------------------------------------//

        // This method retrieves all orders from the database. If an error occurs, it returns an empty list.
        public async Task<IEnumerable<OrderScaled>> GetAllOrdersAsync()
        {
            try
            {
                return await _context.Orders.ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving orders from SQL: {ex.Message}");
                return new List<OrderScaled>();
            }
        }

        //------------------------------------------------------------------------------------------------//

        // This method retrieves an order by its ID from the database. If the order is not found, it returns null.
        public async Task<OrderScaled?> GetOrderByIdAsync(string orderId)
        {
            try
            {
                return await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving order {orderId} from SQL: {ex.Message}");
                return null;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // This method saves an order to the database. If the order already exists, it updates it; otherwise, it adds a new order.
        public async Task<OrderScaled> SaveOrderAsync(OrderScaled order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            try
            {
                var existingOrder = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (existingOrder != null)
                {
                    // Update existing order
                    _context.Entry(existingOrder).CurrentValues.SetValues(order);
                }
                else
                {
                    // Add new order
                    _context.Orders.Add(order);
                }

                await _context.SaveChangesAsync();
                return order;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving order {order.OrderId} to SQL: {ex.Message}");
                throw;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // This method deletes an order from the database based on the provided orderId.
        public async Task<bool> DeleteOrderAsync(string orderId)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                    return false;

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting order {orderId} from SQL: {ex.Message}");
                return false;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // This method updates the status of an order in the database.
        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                    return false;

                order.Status = status;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating status for order {orderId} in SQL: {ex.Message}");
                return false;
            }
        }

        //------------------------------------------------------------------------------------------------//

        // This method retrieves all orders with the status "Completed" from the database.
        public async Task<IEnumerable<OrderScaled>> GetCompletedOrdersAsync()
        {
            try
            {
                return await _context.Orders
                    .Where(o => o.Status == "Completed")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving completed orders from SQL: {ex.Message}");
                return new List<OrderScaled>();
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
