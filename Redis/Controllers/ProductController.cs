using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Redis.Models;
using Redis.Services;

namespace Redis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ICacheService _casheService;
        private readonly ApplicationDbContext _applicationDbContext;
        public ProductController(ICacheService casheService, ApplicationDbContext applicationDbContext)
        {
            _casheService = casheService;
            _applicationDbContext = applicationDbContext;
        }

        [HttpGet("GetProducts")]
        public async Task<IActionResult> GetProducts()
        {
            var casheData = _casheService.GetData<IEnumerable<Product>>("Products");
            if(casheData != null && casheData.Count()>0)
            {
                return Ok(casheData);
            }
            casheData = await _applicationDbContext.Products.ToListAsync();
            var expiryTime = DateTime.Now.AddSeconds(30);
            _casheService.SetData<IEnumerable<Product>>("Products", casheData, expiryTime);
            return Ok(casheData);
        }

        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct(ProductViewModel newproduct)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }
            var product = new Product
            {
                ProductName = newproduct.ProductName,
                ProductDescription = newproduct.ProductDescription,
                Stock = newproduct.Stock,
            };
            var addedObject = await _applicationDbContext.Products.AddAsync(product);
            var expiryTime = DateTime.Now.AddSeconds(30);

            _casheService.SetData<Product>($"product{product.ProductId}", addedObject.Entity, expiryTime);

            await _applicationDbContext.SaveChangesAsync();
            return Ok(addedObject.Entity);
        }

        [HttpDelete("DeleteProduct")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _applicationDbContext.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if(product != null)
            {
                _applicationDbContext.Remove(product);
                _casheService.RemoveData($"product{id}");

                await _applicationDbContext.SaveChangesAsync();
                return NoContent();
            }
            return NotFound();
        }
    }
}
