using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webprogbackend.Attributes;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Models.Enums;

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
            [FromQuery] string category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? inStock = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "name",
            [FromQuery] string sortOrder = "asc")
        {
            var query = _context.Products.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (inStock.HasValue)
            {
                if (inStock.Value)
                    query = query.Where(p => p.StockQuantity > 0);
                else
                    query = query.Where(p => p.StockQuantity == 0);
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "price" => sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.Price)
                    : query.OrderBy(p => p.Price),
                "stock" => sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.StockQuantity)
                    : query.OrderBy(p => p.StockQuantity),
                "created" => sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt),
                _ => sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name)
            };

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

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Update properties
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Category = product.Category;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.UpdatedAt = DateTime.UtcNow;

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

        // POST: api/Products
        [HttpPost]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product is in any orders
            var isInOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (isInOrders)
            {
                return BadRequest("Cannot delete product that exists in orders. Consider setting stock to 0 instead.");
            }

            // Check if product is in any carts
            var isInCarts = await _context.CartItems.AnyAsync(ci => ci.ProductId == id);
            if (isInCarts)
            {
                // Remove from all carts
                var cartItems = await _context.CartItems.Where(ci => ci.ProductId == id).ToListAsync();
                _context.CartItems.RemoveRange(cartItems);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/Featured
        [HttpGet("Featured")]
        public async Task<ActionResult<IEnumerable<Product>>> GetFeaturedProducts([FromQuery] int count = 6)
        {
            var featuredProducts = await _context.Products
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return featuredProducts;
        }

        // GET: api/Products/Search
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search query cannot be empty");
            }

            var searchResults = await _context.Products
                .Where(p => p.Name.Contains(q) ||
                           p.Description.Contains(q) ||
                           p.Category.Contains(q))
                .Take(20)
                .ToListAsync();

            return searchResults;
        }

        // PUT: api/Products/5/Stock
        [HttpPut("{id}/Stock")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockModel model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (model.Quantity < 0)
            {
                return BadRequest("Stock quantity cannot be negative");
            }

            product.StockQuantity = model.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/Stats
        [HttpGet("Stats")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<object>> GetProductStats()
        {
            var totalProducts = await _context.Products.CountAsync();
            var inStockProducts = await _context.Products.CountAsync(p => p.StockQuantity > 0);
            var outOfStockProducts = await _context.Products.CountAsync(p => p.StockQuantity == 0);
            var lowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 10);

            var averagePrice = await _context.Products.AverageAsync(p => p.Price);
            var totalInventoryValue = await _context.Products.SumAsync(p => p.Price * p.StockQuantity);

            var topSellingProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .Join(_context.Products,
                      tp => tp.ProductId,
                      p => p.Id,
                      (tp, p) => new
                      {
                          p.Id,
                          p.Name,
                          p.Category,
                          TotalSold = tp.TotalSold,
                          Revenue = tp.Revenue,
                          CurrentStock = p.StockQuantity
                      })
                .ToListAsync();

            var categoryStats = await _context.Products
                .GroupBy(p => p.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    ProductCount = g.Count(),
                    AveragePrice = g.Average(p => p.Price),
                    TotalValue = g.Sum(p => p.Price * p.StockQuantity),
                    InStockCount = g.Count(p => p.StockQuantity > 0)
                })
                .OrderBy(x => x.Category)
                .ToListAsync();

            var stats = new
            {
                TotalProducts = totalProducts,
                InStockProducts = inStockProducts,
                OutOfStockProducts = outOfStockProducts,
                LowStockProducts = lowStockProducts,
                AveragePrice = averagePrice,
                TotalInventoryValue = totalInventoryValue,
                TopSellingProducts = topSellingProducts,
                CategoryStats = categoryStats
            };

            return stats;
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }

    // DTO Models
    public class UpdateStockModel
    {
        public int Quantity { get; set; }
    }
}