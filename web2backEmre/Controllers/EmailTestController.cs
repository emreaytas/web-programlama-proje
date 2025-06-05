using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webprogbackend.Services;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;
        private readonly IConfiguration _configuration;

        public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger, IConfiguration configuration)
        {
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        // Test email gönder - İYİLEŞTİRİLMİŞ VERSİYON
        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation($"🧪 Test email isteği alındı: {request.Email}");

                // 1. Email adresini doğrula
                if (string.IsNullOrEmpty(request.Email) || !request.Email.Contains("@"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Geçersiz email adresi",
                        timestamp = DateTime.Now
                    });
                }

                // 3. SMTP bağlantısını test et
                _logger.LogInformation("🔧 SMTP bağlantısı test ediliyor...");
                var connectionTest = await _emailService.TestSmtpConnectionAsync();
                if (!connectionTest.IsConnected)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "SMTP bağlantı hatası",
                        details = connectionTest.Message,
                        troubleshooting = GetTroubleshootingSteps(),
                        timestamp = DateTime.Now
                    });
                }

                // 4. Test email'ini gönder
                _logger.LogInformation($"📧 Test email gönderiliyor: {request.Email}");
                await _emailService.SendTestEmailAsync(request.Email);

                var duration = DateTime.Now - startTime;

                _logger.LogInformation($"✅ Test email başarıyla gönderildi! Süre: {duration.TotalMilliseconds}ms");

                return Ok(new
                {
                    success = true,
                    message = $"Test email'i {request.Email} adresine başarıyla gönderildi",
                    details = new
                    {
                        recipientEmail = request.Email,
                        sentAt = DateTime.Now,
                        duration = $"{duration.TotalMilliseconds:F0}ms",
                        smtpServer = _configuration["EmailSettings:SmtpServer"],
                        smtpPort = _configuration["EmailSettings:Port"],
                        fromEmail = _configuration["EmailSettings:FromEmail"]
                    },
                    checkList = new[]
                    {
                        "✅ Email ayarları doğrulandı",
                        "✅ SMTP bağlantısı test edildi",
                        "✅ Email başarıyla gönderildi",
                        "📧 Lütfen gelen kutusu ve spam klasörünü kontrol edin"
                    },
                    importantNotes = new[]
                    {
                        "📁 Email spam/gereksiz klasöründe olabilir",
                        "⏱️ Email'in ulaşması 1-5 dakika sürebilir",
                        "📱 Mobil uygulamadan da kontrol edin",
                        "🔄 Başka bir email adresi ile de test edin"
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (InvalidOperationException ex)
            {
                // Konfigürasyon hataları
                _logger.LogError(ex, $"❌ Email konfigürasyon hatası: {request.Email}");

                return BadRequest(new
                {
                    success = false,
                    message = "Email konfigürasyon hatası",
                    error = ex.Message,
                    configurationHelp = GetConfigurationHelp(),
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // Genel hatalar
                _logger.LogError(ex, $"❌ Test email gönderilirken hata: {request.Email}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Test email gönderilemedi",
                    error = ex.Message,
                    troubleshooting = GetTroubleshootingSteps(),
                    timestamp = DateTime.Now
                });
            }
        }

        // YENİ ENDPOINT - Sipariş E-postalarını Gönder
        [HttpPost("send-order-emails")]
        public async Task<IActionResult> SendOrderEmails([FromBody] OrderEmailRequest request)
        {
            try
            {
                _logger.LogInformation($"📧 Sipariş e-postaları gönderiliyor - Sipariş: {request.OrderNumber}");
                _logger.LogInformation($"📧 Müşteri: {request.CustomerName} ({request.CustomerEmail})");
                _logger.LogInformation($"📧 Toplam Tutar: {request.TotalAmount:C}");

                var customerEmailSent = false;
                var adminEmailSent = false;
                var errors = new List<string>();

                // 1. Müşteri onay e-postası gönder
                try
                {
                    await _emailService.SendOrderConfirmationAsync(
                        request.CustomerEmail,
                        request.OrderNumber,
                        request.TotalAmount
                    );
                    customerEmailSent = true;
                    _logger.LogInformation($"✅ Müşteri onay e-postası gönderildi: {request.CustomerEmail}");
                }
                catch (Exception ex)
                {
                    var error = $"Müşteri e-postası gönderilemedi: {ex.Message}";
                    errors.Add(error);
                    _logger.LogError(ex, error);
                }

                // 2. Admin bilgilendirme e-postası gönder
                try
                {
                    var orderDetailsHtml = CreateOrderDetailsHtml(request);
                    await _emailService.SendNewOrderNotificationAsync(orderDetailsHtml);
                    adminEmailSent = true;
                    _logger.LogInformation("✅ Admin bilgilendirme e-postası gönderildi");
                }
                catch (Exception ex)
                {
                    var error = $"Admin e-postası gönderilemedi: {ex.Message}";
                    errors.Add(error);
                    _logger.LogError(ex, error);
                }

                // 3. Sonucu döndür
                var success = customerEmailSent && adminEmailSent;

                return Ok(new
                {
                    success = success,
                    message = success
                        ? "Tüm e-postalar başarıyla gönderildi"
                        : "Bazı e-postalar gönderilemedi",
                    details = new
                    {
                        customerEmailSent = customerEmailSent,
                        adminEmailSent = adminEmailSent,
                        orderNumber = request.OrderNumber,
                        customerEmail = request.CustomerEmail,
                        totalAmount = request.TotalAmount,
                        sentAt = DateTime.Now
                    },
                    errors = errors,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş e-postaları gönderme genel hatası");
                return StatusCode(500, new
                {
                    success = false,
                    message = "E-posta gönderme servisi hatası",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        // SMTP bağlantısını test et
        [HttpPost("test-smtp-connection")]
        public async Task<IActionResult> TestSmtpConnection()
        {
            try
            {
                _logger.LogInformation("🔧 SMTP bağlantı testi başlatılıyor...");

                var result = await _emailService.TestSmtpConnectionAsync();

                if (result.IsConnected)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "SMTP bağlantısı başarılı",
                        details = result.Message,
                        smtpInfo = new
                        {
                            server = _configuration["EmailSettings:SmtpServer"],
                            port = _configuration["EmailSettings:Port"],
                            ssl = true,
                            authentication = true
                        },
                        timestamp = DateTime.Now
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "SMTP bağlantısı başarısız",
                        error = result.Message,
                        troubleshooting = GetSmtpTroubleshootingSteps(),
                        timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP bağlantı testi sırasında hata");

                return StatusCode(500, new
                {
                    success = false,
                    message = "SMTP bağlantı testi başarısız",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        // Gmail özel testi - GELİŞTİRİLMİŞ VERSİYON
        [HttpPost("test-gmail")]
        public async Task<IActionResult> TestGmailSmtp([FromBody] GmailTestRequest request)
        {
            try
            {
                var testEmail = request.Email ?? _configuration["EmailSettings:DefaultRecipientEmail"] ?? "test@example.com";

                _logger.LogInformation($"📧 Gmail SMTP testi başlatılıyor - Hedef: {testEmail}");

                // Gmail ayarlarını özel olarak kontrol et
                var gmailCheck = CheckGmailConfiguration();
                if (!gmailCheck.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gmail konfigürasyonu hatalı",
                        issues = gmailCheck.Issues,
                        gmailSetupGuide = GetGmailSetupGuide(),
                        timestamp = DateTime.Now
                    });
                }

                await _emailService.SendTestEmailAsync(testEmail);

                _logger.LogInformation("✅ Gmail SMTP testi başarılı");

                return Ok(new
                {
                    success = true,
                    message = $"Gmail SMTP testi başarılı - {testEmail} adresine gönderildi",
                    gmailInfo = new
                    {
                        smtp = "smtp.gmail.com:587",
                        ssl = true,
                        appPasswordUsed = CheckIfAppPasswordFormat(_configuration["EmailSettings:Password"]),
                        testTime = DateTime.Now,
                        targetEmail = testEmail
                    },
                    importantReminders = new[]
                    {
                        "✅ 2-Step Verification aktif olmalı",
                        "🔑 App Password kullanılmalı (normal şifre değil)",
                        "📱 16 haneli App Password doğru girilmeli",
                        "📁 Spam klasörünü kontrol etmeyi unutmayın"
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gmail SMTP testi başarısız");

                return BadRequest(new
                {
                    success = false,
                    message = "Gmail SMTP testi başarısız: " + ex.Message,
                    gmailTroubleshooting = GetGmailTroubleshootingSteps(),
                    commonSolutions = new[]
                    {
                        "🔑 App Password oluşturun: https://myaccount.google.com/apppasswords",
                        "🛡️ 2-Step Verification açın: https://myaccount.google.com/security",
                        "📧 Email adresini doğru yazdığınızdan emin olun",
                        "🔄 Farklı bir Gmail hesabı ile test edin",
                        "⏱️ Birkaç dakika bekleyip tekrar deneyin"
                    },
                    timestamp = DateTime.Now,
                    errorType = ex.GetType().Name
                });
            }
        }

        // Test email formatı
        [HttpPost("test-format")]
        public async Task<IActionResult> TestEmailFormat([FromBody] EmailFormatTestRequest request)
        {
            try
            {
                var subject = $"🎨 Email Format Testi - {DateTime.Now:HH:mm}";
                var body = CreateFormattedTestEmail(request.TestType, request.Email);

                await _emailService.SendAdminNotificationAsync(subject, body);

                return Ok(new
                {
                    success = true,
                    message = "Email format testi gönderildi",
                    features = new[]
                    {
                        "📱 Responsive tasarım",
                        "🎨 Modern CSS3 stilleri",
                        "😀 Emoji desteği",
                        "🌈 Gradient arka planlar",
                        "📊 HTML tablolar"
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        // Private Helper Methods
        private string CreateOrderDetailsHtml(OrderEmailRequest request)
        {
            var itemsHtml = string.Join("", request.Items.Select(item => $@"
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{item.ProductName}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:C}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>{item.TotalPrice:C}</td>
                </tr>
            "));

            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 15px; color: white; text-align: center; margin-bottom: 20px;'>
                        <h1 style='margin: 0; font-size: 28px;'>🛒 Yeni Sipariş Alındı!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px;'>Sipariş Numarası: {request.OrderNumber}</p>
                    </div>
                    
                    <div style='background-color: #f8f9fa; padding: 20px; margin: 20px 0; border-radius: 8px; border-left: 4px solid #28a745;'>
                        <h3 style='margin: 0 0 15px 0; color: #155724;'>👤 Müşteri Bilgileri</h3>
                        <p style='margin: 5px 0;'><strong>Ad Soyad:</strong> {request.CustomerName}</p>
                        <p style='margin: 5px 0;'><strong>E-posta:</strong> {request.CustomerEmail}</p>
                        <p style='margin: 5px 0;'><strong>Telefon:</strong> {request.CustomerPhone}</p>
                        <p style='margin: 5px 0;'><strong>Adres:</strong> {request.ShippingAddress}</p>
                    </div>
                    
                    <div style='background-color: #e7f3ff; padding: 20px; margin: 20px 0; border-radius: 8px; border-left: 4px solid #0066cc;'>
                        <h3 style='margin: 0 0 15px 0; color: #004080;'>📦 Sipariş Detayları</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <thead>
                                <tr style='background-color: #f8f9fa;'>
                                    <th style='padding: 10px; text-align: left; border-bottom: 2px solid #ddd;'>Ürün</th>
                                    <th style='padding: 10px; text-align: center; border-bottom: 2px solid #ddd;'>Adet</th>
                                    <th style='padding: 10px; text-align: right; border-bottom: 2px solid #ddd;'>Birim Fiyat</th>
                                    <th style='padding: 10px; text-align: right; border-bottom: 2px solid #ddd;'>Toplam</th>
                                </tr>
                            </thead>
                            <tbody>
                                {itemsHtml}
                            </tbody>
                            <tfoot>
                                <tr style='background-color: #e8f5e8; font-weight: bold;'>
                                    <td colspan='3' style='padding: 15px; text-align: right; border-top: 2px solid #28a745;'>Genel Toplam:</td>
                                    <td style='padding: 15px; text-align: right; border-top: 2px solid #28a745; color: #28a745; font-size: 18px;'>{request.TotalAmount:C}</td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                    
                    <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-radius: 8px; margin-top: 20px;'>
                        <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                            📅 Sipariş Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss}<br>
                            💻 <strong>E-Commerce Admin Sistemi</strong><br>
                            🔔 Bu bir otomatik bildirimdir
                        </p>
                    </div>
                </div>";
        }

        private (bool IsValid, List<string> Issues) CheckGmailConfiguration()
        {
            var issues = new List<string>();
            var isValid = true;

            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = _configuration["EmailSettings:Port"];
            var password = _configuration["EmailSettings:Password"];

            if (!string.IsNullOrEmpty(fromEmail) && fromEmail.EndsWith("@gmail.com"))
            {
                if (smtpServer != "smtp.gmail.com")
                {
                    issues.Add("❌ Gmail için SMTP server 'smtp.gmail.com' olmalı");
                    isValid = false;
                }

                if (smtpPort != "587")
                {
                    issues.Add("⚠️ Gmail için port '587' önerilir");
                }

                if (!string.IsNullOrEmpty(password))
                {
                    if (password.Length != 16 || password.Contains(" "))
                    {
                        issues.Add("⚠️ Gmail App Password 16 haneli olmalı (boşluk olmadan)");
                    }
                }
                else
                {
                    issues.Add("❌ Gmail App Password gerekli");
                    isValid = false;
                }
            }

            return (isValid, issues);
        }

        private bool CheckIfAppPasswordFormat(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            return password.Length == 16 && !password.Contains(" ");
        }

        private List<string> GetTroubleshootingSteps()
        {
            return new List<string>
            {
                "1. appsettings.json dosyasında EmailSettings bölümünü kontrol edin",
                "2. SMTP server ve port ayarlarını doğrulayın",
                "3. Email adresi ve şifrenin doğru olduğundan emin olun",
                "4. Gmail kullanıyorsanız App Password oluşturun",
                "5. İnternet bağlantınızı kontrol edin",
                "6. Firewall ayarlarını kontrol edin (port 587)",
                "7. Antivirus yazılımının email trafiğini engellemediğinden emin olun"
            };
        }

        private List<string> GetSmtpTroubleshootingSteps()
        {
            return new List<string>
            {
                "🔧 SMTP server adresini kontrol edin",
                "🔌 Port numarasını doğrulayın (genellikle 587 veya 465)",
                "🔐 SSL/TLS ayarlarını kontrol edin",
                "👤 Kullanıcı adı ve şifrenin doğru olduğundan emin olun",
                "🌐 İnternet bağlantınızı test edin",
                "🛡️ Firewall'ın SMTP trafiğini engellememesini sağlayın"
            };
        }

        private List<string> GetGmailTroubleshootingSteps()
        {
            return new List<string>
            {
                "🔑 App Password oluşturun: https://myaccount.google.com/apppasswords",
                "🛡️ 2-Step Verification'ı açın: https://myaccount.google.com/security",
                "📧 Normal şifre değil, App Password kullanın",
                "✏️ App Password'u boşluk olmadan 16 hane olarak girin",
                "🚫 'Less secure app access'i KAPATIN",
                "⏱️ Gmail hesabı geçici bloklanmışsa birkaç saat bekleyin",
                "📱 Gmail mobil uygulamasından giriş yapabildiğinizi test edin"
            };
        }

        private List<string> GetConfigurationHelp()
        {
            return new List<string>
            {
                "appsettings.json dosyasına EmailSettings bölümü ekleyin:",
                "\"EmailSettings\": {",
                "  \"SmtpServer\": \"smtp.gmail.com\",",
                "  \"Port\": \"587\",",
                "  \"FromEmail\": \"your-email@gmail.com\",",
                "  \"Password\": \"your-16-digit-app-password\",",
                "  \"FromName\": \"Your Name\",",
                "  \"DefaultRecipientEmail\": \"admin@yoursite.com\"",
                "}"
            };
        }

        private List<string> GetGmailSetupGuide()
        {
            return new List<string>
            {
                "1. Gmail hesabınızda 2-Step Verification açın",
                "2. https://myaccount.google.com/apppasswords adresine gidin",
                "3. 'Mail' seçin ve App Password oluşturun",
                "4. Oluşturulan 16 haneli kodu appsettings.json'a ekleyin",
                "5. SMTP ayarları: smtp.gmail.com:587, SSL=true"
            };
        }

        private string CreateFormattedTestEmail(string testType, string email)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px;'>
                    <div style='background: white; padding: 30px; border-radius: 15px; box-shadow: 0 10px 30px rgba(0,0,0,0.2);'>
                        <h1 style='color: #333; text-align: center; margin-bottom: 30px;'>
                            🎨 Email Format Testi
                        </h1>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                            <h3 style='color: #495057; margin: 0 0 15px 0;'>📧 Test Detayları</h3>
                            <p><strong>Test Türü:</strong> {testType}</p>
                            <p><strong>Gönderim Zamanı:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
                            <p><strong>Alıcı:</strong> {email}</p>
                        </div>

                        <div style='background: linear-gradient(45deg, #28a745, #20c997); color: white; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                            <h3 style='margin: 0 0 15px 0;'>✨ Format Özellikleri</h3>
                            <ul style='margin: 0; padding-left: 20px;'>
                                <li>📱 Responsive tasarım</li>
                                <li>🎨 Gradient arka planlar</li>
                                <li>🔤 Emoji desteği</li>
                                <li>📐 Modern CSS3 stilleri</li>
                                <li>🎯 Kolay okunabilirlik</li>
                            </ul>
                        </div>

                        <div style='text-align: center; padding: 20px; background: #e9ecef; border-radius: 10px;'>
                            <p style='margin: 0; font-size: 14px; color: #6c757d;'>
                                🚀 Bu email otomatik format testi için gönderilmiştir.<br>
                                💻 E-ticaret Backend Sistemi<br>
                                📅 {DateTime.Now:dd.MM.yyyy HH:mm}
                            </p>
                        </div>
                    </div>
                </div>";
        }

        // DTO Classes
        public class TestEmailRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class GmailTestRequest
        {
            public string? Email { get; set; }
        }

        public class EmailFormatTestRequest
        {
            public string Email { get; set; } = string.Empty;
            public string TestType { get; set; } = "Format Test";
        }

        public class OrderEmailRequest
        {
            public string OrderNumber { get; set; } = string.Empty;
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
            public string CustomerPhone { get; set; } = string.Empty;
            public string ShippingAddress { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public int ItemCount { get; set; }
            public List<OrderEmailItem> Items { get; set; } = new();
        }

        public class OrderEmailItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }
    }
}