using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LogiSyn.Model;
using LogiSyn.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndersonsBakeryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<OrderScaled>> GetOrders()
        {
            return Ok(_orderService.GetOrders());
        }

        [HttpGet("{id}")]
        public ActionResult<OrderScaled> GetOrderById(string id)
        {
            var order = _orderService.GetOrderById(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost("parse")]
        public async Task<ActionResult<OrderScaled>> ParsePdfOrder([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No PDF file uploaded.");

            var tempPath = Path.GetTempFileName();
            try
            {
                await using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var scaledOrder = _orderService.ReadAndScaleOrder(tempPath);
                return Ok(scaledOrder);
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        [HttpPost]
        public ActionResult<OrderScaled> SaveOrder([FromBody] OrderScaled order)
        {
            if (order == null) return BadRequest("Invalid order payload.");

            _orderService.SaveOrder(order);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
        }
    }
}
