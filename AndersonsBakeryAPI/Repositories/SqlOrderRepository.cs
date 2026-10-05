// Adriaan
using SharedLibrary.Interface;
using SharedLibrary.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AndersonsBakeryAPI.Repositories
{
    public class SqlOrderRepository : IOrderRepository
    {
        private readonly string? _connectionString;
        private readonly ILogger<SqlOrderRepository>? _logger;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the SqlOrderRepository class
        public SqlOrderRepository(Data.LogiSynDbContext context, ILogger<SqlOrderRepository>? logger = null)
        {
            _logger = logger;
            try
            {
                _connectionString = context?.Database?.GetConnectionString();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get connection string from DbContext; falling back to localdb.");
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
                _logger?.LogWarning(ex, "Error retrieving orders from SQL");
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
                _logger?.LogWarning(ex, "Error retrieving order {OrderId} from SQL", orderId);
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

                    context.Entry(existingOrder).Property(o => o.ProductionItems).IsModified = true;
                    context.Entry(existingOrder).Property(o => o.RawMaterials).IsModified = true;
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
                _logger?.LogError(ex, "Error saving order {OrderId} to SQL", order.OrderId);
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
                _logger?.LogWarning(ex, "Error deleting order {OrderId} from SQL", orderId);
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
                _logger?.LogWarning(ex, "Error updating status for order {OrderId} in SQL", orderId);
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
                _logger?.LogWarning(ex, "Error retrieving completed orders from SQL");
                return new List<OrderScaled>();
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//