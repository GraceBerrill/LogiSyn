// Adriaan
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharedLibrary.Model;
using SharedLibrary.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndersonsBakeryAPI.Controllers
{
    // Initialise the API controller for handling order-related HTTP requests
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the OrdersController class
        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        //------------------------------------------------------------------------------------------------//

        // GET endpoint to retrieve all orders (supports ?refresh=true)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderScaled>>> GetOrders([FromQuery] bool refresh = false)
        {
            Console.WriteLine($"[API] GET /api/orders called (refresh={refresh})");
            var orders = await _orderService.GetOrdersAsync(refresh);
            Console.WriteLine($"[API] Returning {orders.Count()} orders");
            return Ok(orders);
        }

        //------------------------------------------------------------------------------------------------//

        // GET endpoint to retrieve a specific order by its ID
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderScaled>> GetOrderById(string id)
        {
            Console.WriteLine($"[API] GET /api/orders/{id} called");
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        //------------------------------------------------------------------------------------------------//

        // POST endpoint to parse a PDF order file and return the scaled order
        [HttpPost("parse")]
        public async Task<ActionResult<OrderScaled>> ParsePdfOrder([FromForm] IFormFile file)
        {
            // Log the request and check if a file was uploaded
            Console.WriteLine("[API] POST /api/orders/parse called");
            if (file == null || file.Length == 0)
                return BadRequest("No PDF file uploaded.");

            // Save the uploaded PDF to a temporary file
            var tempPath = Path.GetTempFileName();
            try
            {
                await using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Log the parsing process and call the order service to read and scale the order
                Console.WriteLine($"[API] Parsing PDF from {tempPath}");
                var scaledOrder = _orderService.ReadAndScaleOrder(tempPath);
                Console.WriteLine($"[API] PDF parsed successfully, order ID: {scaledOrder.OrderId}");
                return Ok(scaledOrder);
            }
            catch (Exception ex)
            {
                // Log any errors that occur during parsing and return a BadRequest response
                Console.WriteLine($"[API] ERROR parsing PDF: {ex.Message}");
                return BadRequest($"Error parsing PDF: {ex.Message}");
            }
            finally
            {
                // Clean up the temporary file after processing
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // POST endpoint to save a scaled order to the database
        [HttpPost]
        public async Task<ActionResult<OrderScaled>> SaveOrder([FromBody] OrderScaled order)
        {
            // Log the request and validate the order payload
            Console.WriteLine("[API] POST /api/orders called with SaveOrder");
            if (order == null) return BadRequest("Invalid order payload.");

            // Log the order details and call the order service to save the order
            Console.WriteLine($"[API] Saving order: ID={order.OrderId}, Customer={order.Customer}, Status={order.Status}");
            var saved = await _orderService.SaveOrderAsync(order);
            Console.WriteLine($"[API] Order {saved.OrderId} saved successfully");
            return CreatedAtAction(nameof(GetOrderById), new { id = saved.OrderId }, saved);
        }

        //------------------------------------------------------------------------------------------------//

        // PUT endpoint to update the status of an existing order
        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateOrderStatus(string id, [FromBody] string status)
        {
            Console.WriteLine($"[API] PUT /api/orders/{id}/status called with {status}");
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _orderService.SaveOrderAsync(order);
            return NoContent();
        }

        //------------------------------------------------------------------------------------------------//

        // GET endpoint for debugging purposes, returns a simple ping response with the current timestamp and order count
        [HttpGet("debug/ping")]
        public ActionResult<object> DebugPing()
        {
            Console.WriteLine("[DEBUG] Ping endpoint called");
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                message = "API is running",
                orderCount = _orderService.GetOrders().Count()
            });
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//

