using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Interface;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAllProducts()
        {
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
        public ActionResult<Product> AddProduct([FromBody] Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ProductName))
                return BadRequest("Product data is required.");

            try
            {
                _productService.Add(product);
                return CreatedAtAction(nameof(GetProductByName), new { name = product.ProductName }, product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{name}")]
        public ActionResult<Product> UpdateProduct(string name, [FromBody] Product product)
        {
            if (product == null)
                return BadRequest("Product data is required.");

            if (string.IsNullOrWhiteSpace(product.ProductName))
                product.ProductName = name;

            try
            {
                _productService.UpdateFromDetail(product);
                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{name}")]
        public IActionResult DeleteProduct(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Product name is required.");

            try
            {
                _productService.DeleteByName(name);
                return Ok(new { message = $"Product '{name}' deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}