using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Attributes;
using webprogbackend.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }


        // GET: api/Products/TopStock - En çok stoðu olan 3 ürün (TopSellingCarousel için)
        [HttpGet("TopStock")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopStockProducts([FromQuery] int count = 3)
        {
            try
            {
                var topStockProducts = await _context.Products
                    .Where(p => p.StockQuantity > 0) // Sadece stokta olan ürünler
                    .OrderByDescending(p => p.StockQuantity) // En çok stoðu olana göre sýrala
                    .Take(count)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Price,
                        p.StockQuantity,
                        p.Category,
                        p.ImageUrl,
                        p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(topStockProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "En çok stoðu olan ürünler getirilirken hata oluþtu");
                return StatusCode(500, new { message = "Sunucu hatasý oluþtu" });
            }
        }

        // GET: api/Products/All - Tüm ürünler (Home sayfasý için)
        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string category = "",
            [FromQuery] string search = "",
            [FromQuery] bool inStockOnly = false)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                // Kategoriye göre filtreleme
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category.ToLower() == category.ToLower());
                }

                // Arama filtresi
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
                }

                // Sadece stokta olanlar
                if (inStockOnly)
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }

                // Toplam sayý
                var totalCount = await query.CountAsync();

                // Sayfalama ve sýralama
                var products = await query
                    .OrderByDescending(p => p.CreatedAt) // En yeni ürünler önce
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Price,
                        p.StockQuantity,
                        p.Category,
                        p.ImageUrl,
                        p.CreatedAt,
                        IsInStock = p.StockQuantity > 0,
                        StockStatus = p.StockQuantity == 0 ? "Tükendi" :
                                     p.StockQuantity <= 10 ? "Az Kaldý" : "Stokta"
                    })
                    .ToListAsync();

                // Response headers
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm ürünler getirilirken hata oluþtu");
                return StatusCode(500, new { message = "Sunucu hatasý oluþtu" });
            }
        }


        // GET: api/Products/All
        [HttpGet("All2")]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            try
            {
                var products = await _context.Products
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                return StatusCode(500, new { message = "Ürünler yüklenirken hata oluþtu" });
            }
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new { message = "Ürün bulunamadý" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return StatusCode(500, new { message = "Ürün yüklenirken hata oluþtu" });
            }
        }

        // POST: api/Products
        [HttpPost]
        [Authorize] // Giriþ yapmýþ kullanýcýlar ürün ekleyebilir
        public async Task<ActionResult<Product>> CreateProduct(CreateProductDto productDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = new Product
                {
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    StockQuantity = productDto.StockQuantity,
                    Category = productDto.Category,
                    ImageUrl = productDto.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created: {ProductName} (ID: {ProductId})", product.Name, product.Id);

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "Ürün oluþturulurken hata oluþtu" });
            }
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto productDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Ürün bulunamadý" });
                }

                product.Name = productDto.Name;
                product.Description = productDto.Description;
                product.Price = productDto.Price;
                product.StockQuantity = productDto.StockQuantity;
                product.Category = productDto.Category;
                product.ImageUrl = productDto.ImageUrl;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated: {ProductName} (ID: {ProductId})", product.Name, product.Id);

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new { message = "Ürün güncellenirken hata oluþtu" });
            }
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Ürün bulunamadý" });
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product deleted: {ProductName} (ID: {ProductId})", product.Name, product.Id);

                return Ok(new { message = "Ürün baþarýyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new { message = "Ürün silinirken hata oluþtu" });
            }
        }

        //// GET: api/Products/TopStock/3
        //[HttpGet("TopStock/{count:int?}")]
        //public async Task<ActionResult<IEnumerable<object>>> GetTopStockProducts2(int count = 3)
        //{
        //    try
        //    {
        //        var topStockProducts = await _context.Products
        //            .OrderByDescending(p => p.StockQuantity)
        //            .Take(count)
        //            .Select(p => new
        //            {
        //                p.Id,
        //                p.Name,
        //                p.Category,
        //                p.Price,
        //                p.StockQuantity,
        //                p.ImageUrl,
        //                p.Description
        //            })
        //            .ToListAsync();

        //        return Ok(new
        //        {
        //            message = $"En fazla stoða sahip {count} ürün",
        //            count = topStockProducts.Count,
        //            products = topStockProducts
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting top stock products");
        //        return StatusCode(500, new { message = "En yüksek stoklu ürünler yüklenirken hata oluþtu" });
        //    }
        //}

        // GET: api/Products/LowStock/10
        [HttpGet("LowStock/{threshold:int?}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetLowStockProducts(int threshold = 10)
        {
            try
            {
                var lowStockProducts = await _context.Products
                    .Where(p => p.StockQuantity <= threshold && p.StockQuantity > 0)
                    .OrderBy(p => p.StockQuantity)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Category,
                        p.Price,
                        p.StockQuantity,
                        p.ImageUrl,
                        StockStatus = p.StockQuantity <= 5 ? "Kritik" : "Düþük"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = $"Stok seviyesi {threshold} ve altýnda olan ürünler",
                    threshold,
                    count = lowStockProducts.Count,
                    products = lowStockProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                return StatusCode(500, new { message = "Düþük stoklu ürünler yüklenirken hata oluþtu" });
            }
        }

        // GET: api/Products/OutOfStock
        [HttpGet("OutOfStock")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetOutOfStockProducts()
        {
            try
            {
                var outOfStockProducts = await _context.Products
                    .Where(p => p.StockQuantity == 0)
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Category,
                        p.Price,
                        p.StockQuantity,
                        p.ImageUrl,
                        p.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Tükenen ürünler",
                    count = outOfStockProducts.Count,
                    products = outOfStockProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting out of stock products");
                return StatusCode(500, new { message = "Tükenen ürünler yüklenirken hata oluþtu" });
            }
        }

        // GET: api/Products/Search/iphone
        [HttpGet("Search/{query}")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Arama terimi boþ olamaz" });
                }

                var products = await _context.Products
                    .Where(p => p.Name.Contains(query) ||
                               p.Description.Contains(query) ||
                               p.Category.Contains(query))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return Ok(new
                {
                    message = $"'{query}' için arama sonuçlarý",
                    query,
                    count = products.Count,
                    products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with query: {Query}", query);
                return StatusCode(500, new { message = "Ürün arama sýrasýnda hata oluþtu" });
            }
        }

        // GET: api/Products/Category/Telefon
        [HttpGet("Category/{category}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(string category)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.Category.ToLower() == category.ToLower())
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return Ok(new
                {
                    message = $"'{category}' kategorisindeki ürünler",
                    category,
                    count = products.Count,
                    products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {Category}", category);
                return StatusCode(500, new { message = "Kategori ürünleri yüklenirken hata oluþtu" });
            }
        }

        // GET: api/Products/PriceRange?min=100&max=1000
        [HttpGet("PriceRange")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByPriceRange(
            [FromQuery] decimal? min = null,
            [FromQuery] decimal? max = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                if (min.HasValue)
                    query = query.Where(p => p.Price >= min.Value);

                if (max.HasValue)
                    query = query.Where(p => p.Price <= max.Value);

                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(p => p.Category.ToLower() == category.ToLower());

                var products = await query
                    .OrderBy(p => p.Price)
                    .ToListAsync();

                return Ok(new
                {
                    message = "Fiyat aralýðýndaki ürünler",
                    filters = new { min, max, category },
                    count = products.Count,
                    products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by price range");
                return StatusCode(500, new { message = "Fiyat aralýðý sorgusu sýrasýnda hata oluþtu" });
            }
        }

        // GET: api/Products/Featured/6
        [HttpGet("Featured/{count:int?}")]
        public async Task<ActionResult<IEnumerable<object>>> GetFeaturedProducts(int count = 6)
        {
            try
            {
                // En yüksek stoklu ve en yeni ürünleri featured olarak kabul edelim
                var featuredProducts = await _context.Products
                    .Where(p => p.StockQuantity > 0)
                    .OrderByDescending(p => p.StockQuantity)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(count)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Category,
                        p.Price,
                        p.StockQuantity,
                        p.ImageUrl,
                        p.Description,
                        IsFeatured = true,
                        p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = $"Öne çýkan {count} ürün",
                    count = featuredProducts.Count,
                    products = featuredProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured products");
                return StatusCode(500, new { message = "Öne çýkan ürünler yüklenirken hata oluþtu" });
            }
        }

        // GET: api/Products/Categories
        [HttpGet("Categories")]
        public async Task<ActionResult<IEnumerable<object>>> GetCategories()
        {
            try
            {
                var categories = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        ProductCount = g.Count(),
                        TotalStock = g.Sum(p => p.StockQuantity),
                        AveragePrice = g.Average(p => p.Price),
                        MinPrice = g.Min(p => p.Price),
                        MaxPrice = g.Max(p => p.Price)
                    })
                    .OrderByDescending(c => c.ProductCount)
                    .ToListAsync();

                return Ok(new
                {
                    message = "Tüm kategoriler ve istatistikleri",
                    count = categories.Count,
                    categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "Kategoriler yüklenirken hata oluþtu" });
            }
        }



        // GET: api/Products/Stats
        [HttpGet("Stats")]
        [Authorize]
        public async Task<ActionResult<object>> GetProductStats()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync();
                var inStockProducts = await _context.Products.CountAsync(p => p.StockQuantity > 0);
                var outOfStockProducts = await _context.Products.CountAsync(p => p.StockQuantity == 0);
                var lowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity <= 10 && p.StockQuantity > 0);

                var totalStockValue = await _context.Products
                    .SumAsync(p => p.Price * p.StockQuantity);

                var averagePrice = await _context.Products.AverageAsync(p => p.Price);

                var stats = new
                {
                    TotalProducts = totalProducts,
                    InStockProducts = inStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    LowStockProducts = lowStockProducts,
                    TotalStockValue = totalStockValue,
                    AveragePrice = averagePrice,
                    StockDistribution = new
                    {
                        InStock = Math.Round((double)inStockProducts / totalProducts * 100, 2),
                        OutOfStock = Math.Round((double)outOfStockProducts / totalProducts * 100, 2),
                        LowStock = Math.Round((double)lowStockProducts / totalProducts * 100, 2)
                    }
                };

                return Ok(new
                {
                    message = "Ürün istatistikleri",
                    generatedAt = DateTime.UtcNow,
                    stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product statistics");
                return StatusCode(500, new { message = "Ýstatistikler yüklenirken hata oluþtu" });
            }
        }

        // POST: api/Products/Bulk
        [HttpPost("Bulk")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<object>> CreateBulkProducts([FromBody] List<CreateProductDto> productsDto)
        {
            try
            {
                if (!productsDto.Any())
                {
                    return BadRequest(new { message = "En az bir ürün gönderilmelidir" });
                }

                var products = productsDto.Select(dto => new Product
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    Category = dto.Category,
                    ImageUrl = dto.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk created {Count} products", products.Count);

                return Ok(new
                {
                    message = $"{products.Count} ürün baþarýyla eklendi",
                    count = products.Count,
                    products = products.Select(p => new { p.Id, p.Name, p.Category }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating products");
                return StatusCode(500, new { message = "Toplu ürün ekleme sýrasýnda hata oluþtu" });
            }
        }
    }

    // DTO Classes
    public class CreateProductDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class UpdateProductDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        public string? ImageUrl { get; set; }
    }
}