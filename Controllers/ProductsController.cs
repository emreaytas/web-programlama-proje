using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webprogbackend.Data;
using webprogbackend.Models;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] string searchTerm = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Products.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Add pagination metadata to response headers
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Total-Pages", totalPages.ToString());
            Response.Headers.Add("X-Current-Page", page.ToString());

            return products;
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // GET: api/Products/Category/{category}
        [HttpGet("Category/{category}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(string category)
        {
            return await _context.Products
                .Where(p => p.Category == category)
                .ToListAsync();
        }



        // GET: api/Products/InStock
        [HttpGet("InStock")]
        public async Task<ActionResult<IEnumerable<Product>>> GetInStockProducts()
        {
            return await _context.Products
                .Where(p => p.StockQuantity > 0)
                .ToListAsync();
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PATCH: api/Products/5/Stock
        [HttpPatch("{id}/Stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateModel model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.StockQuantity = model.StockQuantity;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/Stats
        [HttpGet("Stats")]
        public async Task<ActionResult<object>> GetProductStats()
        {
            var stats = new
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalInStock = await _context.Products.CountAsync(p => p.StockQuantity > 0),
                TotalOutOfStock = await _context.Products.CountAsync(p => p.StockQuantity == 0),
                AveragePrice = await _context.Products.AverageAsync(p => p.Price),
                TotalValue = await _context.Products.SumAsync(p => p.Price * p.StockQuantity)
            };

            return stats;
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }

    public class StockUpdateModel
    {
        public int StockQuantity { get; set; }
    }
} 