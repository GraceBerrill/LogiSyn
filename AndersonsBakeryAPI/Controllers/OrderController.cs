// Adriaan
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharedLibrary.Model;
using SharedLibrary.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndersonsBakeryAPI.Controllers
{
    // Initialise the API controller for handling order-related HTTP requests
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController>? _logger;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the OrdersController class
        public OrdersController(IOrderService orderService, ILogger<OrdersController>? logger = null)
        {
            _orderService = orderService;
            _logger = logger;
        }

        //------------------------------------------------------------------------------------------------//

        // GET endpoint to retrieve all orders (supports ?refresh=true)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderScaled>>> GetOrders([FromQuery] bool refresh = false)
        {
            _logger?.LogInformation("GET /api/orders called (refresh={Refresh})", refresh);
            var orders = await _orderService.GetOrdersAsync(refresh);
            _logger?.LogInformation("Returning {Count} orders", orders.Count());
            return Ok(orders);
        }

        //------------------------------------------------------------------------------------------------//

        // GET endpoint to retrieve a specific order by its ID
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderScaled>> GetOrderById(string id)
        {
            _logger?.LogInformation("GET /api/orders/{Id} called", id);
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
            _logger?.LogInformation("POST /api/orders/parse called");
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
                _logger?.LogInformation("Parsing PDF from {Path}", tempPath);
                var scaledOrder = _orderService.ReadAndScaleOrder(tempPath);
                _logger?.LogInformation("PDF parsed successfully, order ID: {OrderId}", scaledOrder.OrderId);
                return Ok(scaledOrder);
            }
            catch (Exception ex)
            {
                // Log any errors that occur during parsing and return a BadRequest response
                _logger?.LogWarning(ex, "ERROR parsing PDF");
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
            _logger?.LogInformation("POST /api/orders called with SaveOrder");
            if (order == null) return BadRequest("Invalid order payload.");

            // Log the order details and call the order service to save the order
            _logger?.LogInformation("Saving order: ID={OrderId}, Customer={Customer}, Status={Status}", order.OrderId, order.Customer, order.Status);
            var saved = await _orderService.SaveOrderAsync(order);
            _logger?.LogInformation("Order {OrderId} saved successfully", saved.OrderId);
            return CreatedAtAction(nameof(GetOrderById), new { id = saved.OrderId }, saved);
        }

        //------------------------------------------------------------------------------------------------//

        // PUT endpoint to update the status of an existing order
        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateOrderStatus(string id, [FromBody] string status)
        {
            _logger?.LogInformation("PUT /api/orders/{Id}/status called with {Status}", id, status);
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _orderService.SaveOrderAsync(order);
            return NoContent();
        }

        //------------------------------------------------------------------------------------------------//

        // GET endpoint for debugging purposes, returns a simple ping response with the current timestamp and order count
        [AllowAnonymous]
        [HttpGet("debug/ping")]
        public ActionResult<object> DebugPing()
        {
            _logger?.LogDebug("Ping endpoint called");
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

