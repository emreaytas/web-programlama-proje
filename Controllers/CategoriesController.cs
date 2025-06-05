using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webprogbackend.Data;
using webprogbackend.Models;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => c)
                .ToListAsync();

            return categories;
        }

        // GET: api/Categories/{category}/products  
        [HttpGet("{category}/products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(
            string category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Products
                .Where(p => p.Category.ToLower() == category.ToLower());

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Total-Pages", totalPages.ToString());
            Response.Headers.Add("X-Current-Page", page.ToString());

            return products;
        }

        // GET: api/Categories/{category}/stats
        [HttpGet("{category}/stats")]
        public async Task<ActionResult<object>> GetCategoryStats(string category)
        {
            var products = await _context.Products
                .Where(p => p.Category.ToLower() == category.ToLower())
                .ToListAsync();

            if (!products.Any())
            {
                return NotFound($"Category '{category}' not found");
            }

            var stats = new
            {
                Category = category,
                TotalProducts = products.Count,
                InStockProducts = products.Count(p => p.StockQuantity > 0),
                OutOfStockProducts = products.Count(p => p.StockQuantity == 0),
                AveragePrice = products.Average(p => p.Price),
                MinPrice = products.Min(p => p.Price),
                MaxPrice = products.Max(p => p.Price),
                TotalStockValue = products.Sum(p => p.Price * p.StockQuantity)
            };

            return stats;
        }
    }
}