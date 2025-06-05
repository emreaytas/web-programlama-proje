using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webprogbackend.Services;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }



        // Sipariş onay email'i test et
        [HttpPost("test-order-confirmation")]
        public async Task<IActionResult> TestOrderConfirmation([FromBody] TestOrderEmailRequest request)
        {
            try
            {
                await _emailService.SendOrderConfirmationAsync(
                    request.Email,
                    "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-001",
                    1299.99m
                );

                return Ok(new
                {
                    success = true,
                    message = "Sipariş onay email'i gönderildi"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Hoş geldin email'i test et
        [HttpPost("test-welcome")]
        public async Task<IActionResult> TestWelcomeEmail([FromBody] TestWelcomeEmailRequest request)
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(request.Email, request.Username);

                return Ok(new
                {
                    success = true,
                    message = "Hoş geldin email'i gönderildi"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Şifre sıfırlama email'i test et
        [HttpPost("test-password-reset")]
        public async Task<IActionResult> TestPasswordReset([FromBody] TestEmailRequest request)
        {
            try
            {
                var resetLink = "https://localhost:7130/reset-password?token=test-token-123";
                await _emailService.SendPasswordResetAsync(request.Email, resetLink);

                return Ok(new
                {
                    success = true,
                    message = "Şifre sıfırlama email'i gönderildi"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Admin bildirimi test et
        [HttpPost("test-admin-notification")]
        [Authorize(Roles = "Admin")] // Sadece admin kullanabilir
        public async Task<IActionResult> TestAdminNotification([FromBody] AdminNotificationRequest request)
        {
            try
            {
                await _emailService.SendAdminNotificationAsync(request.Subject, request.Message);

                return Ok(new
                {
                    success = true,
                    message = "Admin bildirimi gönderildi"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Email ayarlarını kontrol et
        [HttpGet("check-settings")]
        public IActionResult CheckEmailSettings()
        {
            var config = HttpContext.RequestServices.GetService<IConfiguration>();

            return Ok(new
            {
                smtpConfigured = !string.IsNullOrEmpty(config["EmailSettings:SmtpServer"]),
                fromEmailConfigured = !string.IsNullOrEmpty(config["EmailSettings:FromEmail"]),
                passwordConfigured = !string.IsNullOrEmpty(config["EmailSettings:Password"]),
                message = "Email ayarları kontrol edildi"
            });
        }
    }

    // DTO Sınıfları
    public class TestEmailRequest
    {
        public string Email { get; set; }
    }

    public class TestOrderEmailRequest
    {
        public string Email { get; set; }
    }

    public class TestWelcomeEmailRequest
    {
        public string Email { get; set; }
        public string Username { get; set; }
    }

    public class AdminNotificationRequest
    {
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}