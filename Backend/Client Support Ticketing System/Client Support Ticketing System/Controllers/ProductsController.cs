using System;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ClientSupportTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IUnitOfWork unitOfWork,
            ILogger<ProductsController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var products =
                    await _unitOfWork.Products.GetOrderedAsync();

                var response = products
                    .Select(product => new ProductListResponse
                    {
                        Id = product.Id,
                        Name = product.Name
                    })
                    .ToList();

                return Ok(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to retrieve products. TraceId: {TraceId}",
                    HttpContext.TraceIdentifier);

                return StatusCode(500, new
                {
                    StatusCode = 500,
                    Message =
                        "An unexpected error occurred. Please try again later.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}