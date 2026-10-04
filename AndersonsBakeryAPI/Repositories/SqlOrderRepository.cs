// Adriaan
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
        private readonly string? _connectionString;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the SqlOrderRepository class
        public SqlOrderRepository(Data.LogiSynDbContext context)
        {
            try
            {
                _connectionString = context?.Database?.GetConnectionString();
            }
            catch
            {
                _connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;TrustServerCertificate=True;";
            }
        }

        //------------------------------------------------------------------------------------------------//

        // This method creates a new instance of the db using the string
        private Data.LogiSynDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<Data.LogiSynDbContext>();
            string conn = !string.IsNullOrWhiteSpace(_connectionString)
                ? _connectionString
                : @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;TrustServerCertificate=True;";
            optionsBuilder.UseSqlServer(conn);
            return new Data.LogiSynDbContext(optionsBuilder.Options);
        }

        //------------------------------------------------------------------------------------------------//

        // This method retrieves all orders from the database. If an error occurs, it returns an empty list.
        public async Task<IEnumerable<OrderScaled>> GetAllOrdersAsync()
        {
            try
            {
                await using var context = CreateContext();
                return await context.Orders.AsNoTracking().ToListAsync();
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
                await using var context = CreateContext();
                return await context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId);
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
                await using var context = CreateContext();
                var existingOrder = await context.Orders.FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (existingOrder != null)
                {
                    existingOrder.Customer = order.Customer;
                    existingOrder.OrderDate = order.OrderDate;
                    existingOrder.Status = order.Status;
                    existingOrder.ProductionItems = order.ProductionItems;
                    existingOrder.RawMaterials = order.RawMaterials;
                }
                else
                {
                    context.Orders.Add(order);
                }

                await context.SaveChangesAsync();
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
                await using var context = CreateContext();
                var order = await context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                    return false;

                context.Orders.Remove(order);
                await context.SaveChangesAsync();
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
                await using var context = CreateContext();
                var order = await context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                    return false;

                order.Status = status;
                await context.SaveChangesAsync();
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
                await using var context = CreateContext();
                return await context.Orders.AsNoTracking()
                    .Where(o => o.Status == "Completed" || o.Status == "Complete")
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
