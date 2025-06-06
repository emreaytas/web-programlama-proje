using System.Net;
using System.Net.Mail;

namespace webprogbackend.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string customerEmail, string orderNumber, decimal totalAmount);
        Task SendPasswordResetAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string username);
        Task SendAdminNotificationAsync(string subject, string message);
        Task SendNewOrderNotificationAsync(string orderDetails);
        Task SendTestEmailAsync(string testEmail);
        Task<bool> SendOrderEmailsAsync(string customerEmail, string orderNumber, decimal totalAmount, string orderDetails);
        Task<(bool IsConnected, string Message)> TestSmtpConnectionAsync();
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Test email metodu - DÜZELTME: Daha detaylı hata yakalama
        public async Task SendTestEmailAsync(string testEmail)
        {
            var subject = $"🧪 Test Email - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 15px; color: white; text-align: center;'>
                        <h1 style='margin: 0; font-size: 28px;'>✅ Test Email Başarılı!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px;'>Email sistemi doğru çalışıyor</p>
                    </div>
                    
                    <div style='background-color: #f8f9fa; padding: 20px; margin: 20px 0; border-radius: 8px; border-left: 4px solid #28a745;'>
                        <h3 style='margin: 0 0 15px 0; color: #155724;'>📊 Test Bilgileri</h3>
                        <p style='margin: 5px 0;'><strong>📧 Test Email:</strong> {testEmail}</p>
                        <p style='margin: 5px 0;'><strong>📅 Gönderim Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
                        <p style='margin: 5px 0;'><strong>🌍 UTC Zamanı:</strong> {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}</p>
                        <p style='margin: 5px 0;'><strong>🖥️ Server:</strong> {Environment.MachineName}</p>
                        <p style='margin: 5px 0;'><strong>🌐 Environment:</strong> {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}</p>
                    </div>
                    
                    <div style='background-color: #e7f3ff; padding: 20px; margin: 20px 0; border-radius: 8px; border-left: 4px solid #0066cc;'>
                        <h3 style='margin: 0 0 15px 0; color: #004080;'>⚙️ SMTP Ayarları</h3>
                        <p style='margin: 5px 0;'><strong>📡 SMTP Server:</strong> {_configuration["EmailSettings:SmtpServer"]}</p>
                        <p style='margin: 5px 0;'><strong>🔌 Port:</strong> {_configuration["EmailSettings:Port"]}</p>
                        <p style='margin: 5px 0;'><strong>📨 From Email:</strong> {_configuration["EmailSettings:FromEmail"]}</p>
                        <p style='margin: 5px 0;'><strong>🔐 SSL Enabled:</strong> ✅ True</p>
                        <p style='margin: 5px 0;'><strong>⏱️ Timeout:</strong> 30 saniye</p>
                    </div>
                    
                    <div style='background-color: #fff3cd; padding: 20px; margin: 20px 0; border-radius: 8px; border-left: 4px solid #ffc107;'>
                        <h3 style='margin: 0 0 15px 0; color: #856404;'>💡 Email Gelmiyorsa Kontrol Edin</h3>
                        <ul style='margin: 0; padding-left: 20px; color: #856404;'>
                            <li>📁 <strong>Spam/Gereksiz</strong> klasörünü kontrol edin</li>
                            <li>📧 Email adresinin doğru yazıldığından emin olun</li>
                            <li>⏳ Birkaç dakika bekleyin (gecikmeler olabilir)</li>
                            <li>🔄 Başka bir email adresi ile test edin</li>
                            <li>📱 Mobil uygulamadan da kontrol edin</li>
                        </ul>
                    </div>
                    
                    <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-radius: 8px; margin-top: 20px;'>
                        <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                            🚀 Bu test email'ini aldıysanız sistem doğru çalışıyor!<br>
                            💻 <strong>E-Commerce Email Sistemi</strong><br>
                            📧 Backend tarafından otomatik gönderildi
                        </p>
                    </div>
                </div>";

            try
            {
                await SendEmailAsync(testEmail, subject, body);
                _logger.LogInformation($"✅ Test email başarıyla gönderildi: {testEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Test email gönderilemedi: {testEmail}");
                throw; // Exception'ı tekrar fırlat ki controller'da yakalanabilsin
            }
        }

        // Ana email gönderme metodu - İYİLEŞTİRME: Daha detaylı logging ve hata yakalama
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 1. ADIM: Email ayarlarını al ve doğrula
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPortStr = _configuration["EmailSettings:Port"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:Password"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "E-Commerce Mağaza";

                _logger.LogInformation($"📧 Email gönderimi başlatılıyor...");
                _logger.LogInformation($"   📧 To: {toEmail}");
                _logger.LogInformation($"   📨 Subject: {subject}");
                _logger.LogInformation($"   📡 SMTP: {smtpServer}:{smtpPortStr}");
                _logger.LogInformation($"   📤 From: {fromEmail}");

                // Ayarları doğrula
                if (string.IsNullOrEmpty(smtpServer))
                    throw new InvalidOperationException("SMTP Server yapılandırılmamış! appsettings.json'da EmailSettings:SmtpServer ayarlayın.");

                if (string.IsNullOrEmpty(smtpPortStr) || !int.TryParse(smtpPortStr, out int smtpPort))
                    throw new InvalidOperationException("SMTP Port geçersiz! appsettings.json'da EmailSettings:Port ayarlayın.");

                if (string.IsNullOrEmpty(fromEmail))
                    throw new InvalidOperationException("From Email yapılandırılmamış! appsettings.json'da EmailSettings:FromEmail ayarlayın.");

                if (string.IsNullOrEmpty(fromPassword))
                    throw new InvalidOperationException("Email Password yapılandırılmamış! appsettings.json'da EmailSettings:Password ayarlayın.");

                if (!fromEmail.Contains("@"))
                    throw new InvalidOperationException($"From Email geçersiz format: {fromEmail}");

                // 2. ADIM: SMTP Client oluştur
                _logger.LogInformation("🔧 SMTP Client oluşturuluyor...");

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true,
                    Timeout = 60000, // 60 saniye timeout (artırıldı)
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false // Bu önemli!
                };

                _logger.LogInformation($"✅ SMTP Client hazır: {smtpServer}:{smtpPort}, SSL=true, Timeout=60s");

                // 3. ADIM: Email mesajı oluştur
                _logger.LogInformation("📝 Email mesajı oluşturuluyor...");

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                mailMessage.To.Add(new MailAddress(toEmail));

                // Email başlıkları ekle (spam filtrelerinden kaçınmak için)
                mailMessage.Headers.Add("X-Mailer", "E-Commerce-Backend-v1.0");
                mailMessage.Headers.Add("X-Priority", "3");
                mailMessage.Headers.Add("Message-ID", $"<{Guid.NewGuid()}@{smtpServer}>");

                _logger.LogInformation($"✅ Email mesajı hazır: {mailMessage.To.Count} alıcı, HTML={mailMessage.IsBodyHtml}");

                // 4. ADIM: Email'i gönder
                _logger.LogInformation("🚀 Email gönderiliyor...");

                await smtpClient.SendMailAsync(mailMessage);

                stopwatch.Stop();
                _logger.LogInformation($"✅ Email başarıyla gönderildi! Süre: {stopwatch.ElapsedMilliseconds}ms");
                _logger.LogInformation($"   📧 To: {toEmail}");
                _logger.LogInformation($"   📨 Subject: {subject}");
                _logger.LogInformation($"   ⏱️ Gönderim zamanı: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // 5. ADIM: Başarı durumunda ek bilgiler
                _logger.LogInformation("💡 Email gelmiyorsa kontrol edilecekler:");
                _logger.LogInformation("   1. Spam/Gereksiz klasörünü kontrol edin");
                _logger.LogInformation("   2. Email adresi doğru yazıldı mı?");
                _logger.LogInformation("   3. Birkaç dakika bekleyin (gecikmeler olabilir)");
                _logger.LogInformation("   4. Gmail ise App Password kullandığınızdan emin olun");
            }
            catch (SmtpException smtpEx)
            {
                stopwatch.Stop();
                _logger.LogError($"❌ SMTP Hatası! Süre: {stopwatch.ElapsedMilliseconds}ms");
                _logger.LogError($"   🔴 StatusCode: {smtpEx.StatusCode}");
                _logger.LogError($"   💬 Message: {smtpEx.Message}");

        
                if (smtpEx.Message.Contains("Authentication") || smtpEx.Message.Contains("Username and Password"))
                {
                    throw new InvalidOperationException($"Email kimlik doğrulama hatası! Kullanıcı adı veya şifre yanlış. Gmail kullanıyorsanız App Password kullandığınızdan emin olun. Hata: {smtpEx.Message}", smtpEx);
                }
                else if (smtpEx.Message.Contains("timeout"))
                {
                    throw new InvalidOperationException($"SMTP bağlantı zaman aşımı! İnternet bağlantısını ve firewall ayarlarını kontrol edin. Hata: {smtpEx.Message}", smtpEx);
                }
                else
                {
                    throw new InvalidOperationException($"SMTP hatası: {smtpEx.Message} (StatusCode: {smtpEx.StatusCode})", smtpEx);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"❌ Genel Email Hatası! Süre: {stopwatch.ElapsedMilliseconds}ms");
                _logger.LogError($"   🔴 Exception Type: {ex.GetType().Name}");
                _logger.LogError($"   💬 Message: {ex.Message}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"   🔗 Inner Exception: {ex.InnerException.Message}");
                }

                throw new InvalidOperationException($"Email gönderilemedi: {ex.Message}", ex);
            }
        }

        // SMTP bağlantısını test et - YENİ METOD
        public async Task<(bool IsConnected, string Message)> TestSmtpConnectionAsync()
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPortStr = _configuration["EmailSettings:Port"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:Password"];

                if (!int.TryParse(smtpPortStr, out int smtpPort))
                {
                    return (false, "SMTP Port geçersiz");
                }

                _logger.LogInformation($"🔧 SMTP bağlantısı test ediliyor: {smtpServer}:{smtpPort}");

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true,
                    Timeout = 30000,
                    UseDefaultCredentials = false
                };

                // Sadece bağlantıyı test et (email göndermeden)
                // SmtpClient'ta direkt connect metodu yok, bu yüzden basit bir test email deneriz
                using var testMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = "Connection Test",
                    Body = "Test",
                    IsBodyHtml = false
                };

                testMessage.To.Add(fromEmail); // Kendine test email

                // Bu aslında email gönderir ama test amaçlı
                await smtpClient.SendMailAsync(testMessage);

                _logger.LogInformation("✅ SMTP bağlantısı başarılı");
                return (true, "SMTP bağlantısı başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SMTP bağlantı testi başarısız");
                return (false, $"SMTP bağlantı hatası: {ex.Message}");
            }
        }

        // Diğer metodlar aynı kalıyor (sipariş onayı, şifre sıfırlama, vb.)
        public async Task SendOrderConfirmationAsync(string customerEmail, string orderNumber, decimal totalAmount)
        {
            var subject = $"✅ Sipariş Onayı - {orderNumber}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f8f9fa; padding: 20px;'>
                    <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color: #28a745; text-align: center; margin-bottom: 30px;'>
                            🎉 Siparişiniz Alındı!
                        </h2>
                        
                        <div style='background-color: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                            <h3 style='margin: 0 0 15px 0; color: #0056b3;'>📋 Sipariş Detayları</h3>
                            <p><strong>Sipariş Numarası:</strong> {orderNumber}</p>
                            <p><strong>Toplam Tutar:</strong> {totalAmount:C}</p>
                            <p><strong>Sipariş Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
                        </div>
                        
                        <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-radius: 8px;'>
                            <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                                Teşekkür ederiz! 🛒<br>
                                <strong>E-Commerce Ekibi</strong>
                            </p>
                        </div>
                    </div>
                </div>";

            await SendEmailAsync(customerEmail, subject, body);
        }

        public async Task SendPasswordResetAsync(string email, string resetLink)
        {
            var subject = "🔐 Şifre Sıfırlama Talebi";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #dc3545;'>Şifre Sıfırlama</h2>
                    <p>Şifrenizi sıfırlama talebiniz alınmıştır.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            🔄 Şifremi Sıfırla
                        </a>
                    </div>
                    <p style='color: #dc3545; font-weight: bold;'>⚠️ Bu bağlantı 24 saat geçerlidir.</p>
                </div>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string email, string username)
        {
            var subject = "🎉 Hoş Geldiniz!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>Hoş Geldiniz {username}! 🎊</h2>
                    <p>E-ticaret platformumuza katıldığınız için teşekkür ederiz!</p>
                </div>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendAdminNotificationAsync(string subject, string message)
        {
            var adminEmail = _configuration["EmailSettings:DefaultRecipientEmail"] ??
                           _configuration["AdminSettings:DefaultAdminEmail"];

            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("Admin email adresi yapılandırılmamış!");
                return;
            }

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #fd7e14;'>🔔 Admin Bildirimi</h2>
                    <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                        {message}
                    </div>
                    <p style='color: #666; font-size: 14px;'>
                        Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}<br>
                        Sistem: E-Commerce Backend
                    </p>
                </div>";

            await SendEmailAsync(adminEmail, $"[ADMIN] {subject}", body);
        }

        public async Task SendNewOrderNotificationAsync(string orderDetails)
        {
            var adminEmail = _configuration["EmailSettings:DefaultRecipientEmail"];
            if (string.IsNullOrEmpty(adminEmail)) return;

            var subject = "🛒 Yeni Sipariş Alındı!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>🎉 Yeni Sipariş Alındı!</h2>
                    {orderDetails}
                </div>";

            await SendEmailAsync(adminEmail, subject, body);
        }

        public async Task<bool> SendOrderEmailsAsync(string customerEmail, string orderNumber, decimal totalAmount, string orderDetails)
        {
            try
            {
                await SendOrderConfirmationAsync(customerEmail, orderNumber, totalAmount);
                await SendNewOrderNotificationAsync(orderDetails);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş emailları gönderilemedi");
                return false;
            }
        }

        public async Task<bool> TestEmailConfigurationAsync()
        {
            try
            {
                var testResult = await TestSmtpConnectionAsync();
                return testResult.IsConnected;
            }
            catch
            {
                return false;
            }
        }
    }
}