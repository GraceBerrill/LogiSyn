using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedLibrary.Interface;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAllProducts()
        {
            _logger.LogInformation("GET /api/products called");
            var products = _productService.GetAllProducts();
            return Ok(products);
        }

        [HttpGet("{name}")]
        public ActionResult<Product> GetProductByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Product name is required.");

            var product = _productService.GetProductByName(name);
            if (product == null)
                return NotFound(new { message = $"Product '{name}' not found." });

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<Product> AddProduct([FromBody] Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ProductName))
                return BadRequest("Product data is required.");

            try
            {
                _logger.LogInformation("Adding product: {ProductName}", product.ProductName);
                _productService.Add(product);
                return CreatedAtAction(nameof(GetProductByName), new { name = product.ProductName }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductName}", product.ProductName);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{name}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<Product> UpdateProduct(string name, [FromBody] Product product)
        {
            if (product == null)
                return BadRequest("Product data is required.");

            if (string.IsNullOrWhiteSpace(product.ProductName))
                product.ProductName = name;

            var existing = _productService.GetProductByName(name);
            if (existing == null)
                return NotFound(new { message = $"Product '{name}' not found." });

            try
            {
                _logger.LogInformation("Updating product: {ProductName}", name);
                _productService.UpdateFromDetail(product);
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductName}", name);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{name}")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult DeleteProduct(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Product name is required.");

            var existing = _productService.GetProductByName(name);
            if (existing == null)
                return NotFound(new { message = $"Product '{name}' not found." });

            try
            {
                _logger.LogInformation("Deleting product: {ProductName}", name);
                _productService.DeleteByName(name);
                return Ok(new { message = $"Product '{name}' deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductName}", name);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}