using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using webprogbackend.Data;
using webprogbackend.Models;

namespace webprogbackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UrunController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UrunController> _logger;

        public UrunController(ApplicationDbContext context, ILogger<UrunController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Urun - Kullanıcının sepetindeki ürünleri getir


        // POST: api/Urun - Sepete ürün ekle
        [HttpPost]
        public async Task<ActionResult<object>> AddUrun([FromBody] AddUrunRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Ürünün varlığını kontrol et
                var product = await _context.Products.FindAsync(request.UrunId);
                if (product == null)
                {
                    return NotFound(new { error = "Ürün bulunamadı" });
                }

                // Stok kontrolü
                if (product.StockQuantity <= 0)
                {
                    return BadRequest(new { error = "Ürün stokta bulunmuyor" });
                }

                // Kullanıcının bu ürünü daha önce sepete ekleyip eklemediğini kontrol et
                var existingUrun = await _context.Uruns
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.UrunId == request.UrunId);

                if (existingUrun != null)
                {
                    return BadRequest(new { error = "Bu ürün zaten sepetinizde mevcut" });
                }

                // Yeni ürün kaydı oluştur
                var urun = new Urun
                {
                    UserId = userId,
                    UrunId = request.UrunId,
                    EklenmeTarihi = DateTime.UtcNow
                };

                _context.Uruns.Add(urun);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Kullanıcı {userId} sepetine ürün {request.UrunId} ekledi");

                return Ok(new
                {
                    success = true,
                    message = "Ürün sepete başarıyla eklendi",
                    urunId = urun.Id,
                    productName = product.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün sepete eklenirken hata oluştu");
                return StatusCode(500, new { error = "Ürün sepete eklenirken bir hata oluştu" });
            }
        }

       

      

        // DELETE: api/Urun/clear - Sepeti temizle
        [HttpDelete("clear")]
        public async Task<ActionResult<object>> ClearUruns()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var uruns = await _context.Uruns
                    .Where(u => u.UserId == userId)
                    .ToListAsync();

                if (!uruns.Any())
                {
                    return Ok(new { success = true, message = "Sepet zaten boş" });
                }

                var removedCount = uruns.Count;
                _context.Uruns.RemoveRange(uruns);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Kullanıcı {userId} sepetini temizledi. {removedCount} ürün silindi");

                return Ok(new
                {
                    success = true,
                    message = "Sepet başarıyla temizlendi",
                    removedCount = removedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet temizlenirken hata oluştu");
                return StatusCode(500, new { error = "Sepet temizlenirken bir hata oluştu" });
            }
        }

        // GET: api/Urun/count - Sepetteki ürün sayısı
        [HttpGet("count")]
        public async Task<ActionResult<object>> GetUrunCount()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var count = await _context.Uruns
                    .CountAsync(u => u.UserId == userId);

                return Ok(new { count = count, userId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet sayısı getirilirken hata oluştu");
                return StatusCode(500, new { error = "Sepet sayısı getirilirken bir hata oluştu" });
            }
        }

        // GET: api/Urun/exists/{productId} - Ürünün sepette olup olmadığını kontrol et
        [HttpGet("exists/{productId}")]
        public async Task<ActionResult<object>> CheckUrunExists(int productId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var exists = await _context.Uruns
                    .AnyAsync(u => u.UserId == userId && u.UrunId == productId);

                return Ok(new { exists = exists, productId = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün varlığı kontrol edilirken hata oluştu");
                return StatusCode(500, new { error = "Ürün varlığı kontrol edilirken bir hata oluştu" });
            }
        }

  
    }

    // DTO Sınıfları
    public class AddUrunRequest
    {
        public int UrunId { get; set; } = 0;
    }
}