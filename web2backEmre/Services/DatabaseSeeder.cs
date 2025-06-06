// Services/DatabaseSeeder.cs - Düzeltilmiş versiyon

using Microsoft.EntityFrameworkCore;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Models.Enums;

namespace webprogbackend.Services
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync();
    }

    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Seed admin user
                await SeedAdminUserAsync();

                // Seed test users with different roles
                await SeedTestUsersAsync();

                // Seed sample products if none exist
                await SeedSampleProductsAsync();

                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                // Hatayı yeniden fırlatma, sadece logla
                // throw;
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var adminSettings = _configuration.GetSection("AdminSettings");
            var adminEmail = adminSettings["DefaultAdminEmail"];
            var adminUsername = adminSettings["DefaultAdminUserName"];
            var adminPassword = adminSettings["DefaultAdminPassword"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogWarning("Admin settings not configured properly. Skipping admin user creation.");
                return;
            }

            // Check if admin user already exists
            var existingAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail || u.Role == UserRole.Admin);

            if (existingAdmin != null)
            {
                _logger.LogInformation("Admin user already exists. Skipping admin user creation.");
                return;
            }

            var adminUser = new User
            {
                Username = adminUsername,
                Email = adminEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Admin user created successfully with email: {adminEmail}");
        }

        private async Task SeedTestUsersAsync()
        {
            // Test kullanıcıları oluştur (sadece development ortamında)
            if (!_configuration.GetValue<bool>("SeedTestData", false))
            {
                return;
            }

            var testUsers = new[]
            {
                new { Username = "testuser", Email = "user@test.com", Password = "Test123!", Role = UserRole.User },
                new { Username = "testadmin2", Email = "admin2@test.com", Password = "Test123!", Role = UserRole.Admin }
            };

            foreach (var testUser in testUsers)
            {
                // Kullanıcı zaten var mı kontrol et
                if (!await _context.Users.AnyAsync(u => u.Email == testUser.Email))
                {
                    var user = new User
                    {
                        Username = testUser.Username,
                        Email = testUser.Email,
                        Password = BCrypt.Net.BCrypt.HashPassword(testUser.Password),
                        Role = testUser.Role,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);

                    // Önce kullanıcıyı kaydet
                    await _context.SaveChangesAsync();

                    // Şimdi cart oluştur (normal kullanıcılar için)
                    if (testUser.Role == UserRole.User)
                    {
                        var cart = new Cart
                        {
                            UserId = user.Id, // Artık user.Id var
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Carts.Add(cart);
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation($"Test user created: {testUser.Email} with role: {testUser.Role}");
                }
            }
        }

        private async Task SeedSampleProductsAsync()
        {
            if (await _context.Products.AnyAsync())
            {
                _logger.LogInformation("Products already exist. Skipping sample product creation.");
                return;
            }

            var sampleProducts = new List<Product>
    {
        new Product
        {
            Name = "MacBook Pro 14",
            Description = "Apple MacBook Pro 14 inç, M3 Pro çip, 18GB RAM, 512GB SSD. Profesyonel iş yükü için mükemmel performans.",
            Price = 45999.99m,
            StockQuantity = 15,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Dell XPS 13 Plus",
            Description = "Intel Core i7-1360P, 32GB RAM, 1TB SSD, 13.4 inç OLED dokunmatik ekran.",
            Price = 35999.99m,
            StockQuantity = 22,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "iPhone 15 Pro Max",
            Description = "Apple iPhone 15 Pro Max, 256GB, Titanium Blue. A17 Pro çip, Pro kamera sistemi.",
            Price = 52999.99m,
            StockQuantity = 35,
            Category = "Telefon",
            ImageUrl = "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Samsung Galaxy S24 Ultra",
            Description = "Samsung Galaxy S24 Ultra, 512GB, Titanium Black. Snapdragon 8 Gen 3, S Pen dahil.",
            Price = 48999.99m,
            StockQuantity = 28,
            Category = "Telefon",
            ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Sony WH-1000XM5",
            Description = "Sony WH-1000XM5 Kablosuz Noise Cancelling Kulaklık. 30 saat pil ömrü, premium ses kalitesi.",
            Price = 12999.99m,
            StockQuantity = 45,
            Category = "Ses",
            ImageUrl = "https://images.unsplash.com/photo-1578319439584-104c94d37305?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "AirPods Pro 2. Nesil",
            Description = "Apple AirPods Pro (2. nesil) MagSafe şarj kutusu ile. Adaptif Transparency, Kişiselleştirilmiş Spatial Audio.",
            Price = 8999.99m,
            StockQuantity = 67,
            Category = "Ses",
            ImageUrl = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "iPad Pro 12.9 M2",
            Description = "Apple iPad Pro 12.9 inç, M2 çip, 256GB WiFi + Cellular. Liquid Retina XDR ekran.",
            Price = 38999.99m,
            StockQuantity = 18,
            Category = "Tablet",
            ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Samsung Galaxy Tab S9 Ultra",
            Description = "Samsung Galaxy Tab S9 Ultra, 512GB, 14.6 inç Dynamic AMOLED 2X ekran, S Pen dahil.",
            Price = 34999.99m,
            StockQuantity = 12,
            Category = "Tablet",
            ImageUrl = "https://images.unsplash.com/photo-1561154464-82e9adf32764?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Apple Watch Series 9",
            Description = "Apple Watch Series 9 GPS + Cellular, 45mm, Midnight Aluminum Case. watchOS 10, Siri sorguları.",
            Price = 16999.99m,
            StockQuantity = 33,
            Category = "Giyilebilir",
            ImageUrl = "https://images.unsplash.com/photo-1434493789847-2f02dc6ca35d?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Samsung Galaxy Watch6 Classic",
            Description = "Samsung Galaxy Watch6 Classic, 47mm, Stainless Steel. Gelişmiş sağlık takibi, döner çerçeve.",
            Price = 12999.99m,
            StockQuantity = 26,
            Category = "Giyilebilir",
            ImageUrl = "https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Samsung T7 Portable SSD 2TB",
            Description = "Samsung T7 Portable SSD 2TB. USB 3.2 Gen 2, 1050MB/s okuma hızı, AES 256-bit şifreleme.",
            Price = 4999.99m,
            StockQuantity = 55,
            Category = "Depolama",
            ImageUrl = "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
    
        new Product
        {
            Name = "Logitech MX Master 3S",
            Description = "Logitech MX Master 3S Wireless Mouse. Sessiz tıklama, MagSpeed elektromanyetik kaydırma.",
            Price = 3299.99m,
            StockQuantity = 78,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Keychron K8 Pro Mechanical Keyboard",
            Description = "Keychron K8 Pro Kablosuz Mekanik Klavye. Gateron Pro anahtarlar, RGB backlighting.",
            Price = 4599.99m,
            StockQuantity = 42,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "LG UltraGear 27GP950-B",
            Description = "LG UltraGear 27 inç 4K UHD Nano IPS Gaming Monitör. 144Hz, HDR600, USB-C 90W.",
            Price = 23999.99m,
            StockQuantity = 14,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Nintendo Switch OLED",
            Description = "Nintendo Switch OLED Model. 7 inç OLED ekran, gelişmiş ses kalitesi, 64GB dahili hafıza.",
            Price = 11999.99m,
            StockQuantity = 29,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1606144042614-b2417e99c4e3?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "PlayStation 5",
            Description = "Sony PlayStation 5 Gaming Console. SSD depolama, ray tracing, 4K gaming destekli.",
            Price = 19999.99m,
            StockQuantity = 8,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1606144042614-b2417e99c4e3?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Xbox Series X",
            Description = "Microsoft Xbox Series X. 12 teraflops, 4K 120fps, Quick Resume özellikli.",
            Price = 17999.99m,
            StockQuantity = 12,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1621259182978-fbf93132d53d?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Bose QuietComfort 45",
            Description = "Bose QuietComfort 45 Wireless Headphones. Aktif gürültü engelleme, 24 saat pil ömrü.",
            Price = 10999.99m,
            StockQuantity = 36,
            Category = "Ses",
            ImageUrl = "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "JBL Charge 5",
            Description = "JBL Charge 5 Taşınabilir Bluetooth Hoparlör. Su geçirmez, 20 saat pil ömrü, powerbank özelliği.",
            Price = 3799.99m,
            StockQuantity = 58,
            Category = "Ses",
            ImageUrl = "https://images.unsplash.com/photo-1545454675-3531b543be5d?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Canon EOS R6 Mark II",
            Description = "Canon EOS R6 Mark II Aynasız Fotoğraf Makinesi. 24.2MP, 4K 60p video, dual pixel CMOS AF.",
            Price = 84999.99m,
            StockQuantity = 6,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1606983340126-99ab4feaa64a?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Sony Alpha 7 IV",
            Description = "Sony Alpha 7 IV Tam Kare Aynasız Kamera. 33MP, 4K 60p, real-time tracking.",
            Price = 79999.99m,
            StockQuantity = 9,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "GoPro Hero 12 Black",
            Description = "GoPro Hero 12 Black Action Camera. 5.3K video, HyperSmooth 6.0, su geçirmez.",
            Price = 14999.99m,
            StockQuantity = 21,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1551698618-1dfe5d97d256?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "DJI Air 3",
            Description = "DJI Air 3 Drone. Dual kamera sistemi, 46 dakika uçuş süresi, 4K/60fps video.",
            Price = 34999.99m,
            StockQuantity = 7,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1473968512647-3e447244af8f?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Razer DeathAdder V3 Pro",
            Description = "Razer DeathAdder V3 Pro Gaming Mouse. Focus Pro 30K sensör, 90 saat pil ömrü.",
            Price = 4299.99m,
            StockQuantity = 44,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "SteelSeries Arctis Nova Pro",
            Description = "SteelSeries Arctis Nova Pro Gaming Headset. Aktif gürültü engelleme, premium hi-res ses.",
            Price = 8999.99m,
            StockQuantity = 23,
            Category = "Ses",
            ImageUrl = "https://images.unsplash.com/photo-1618366712010-f4ae9c647dcb?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "ASUS ROG Swift PG32UQX",
            Description = "ASUS ROG Swift 32 inç 4K Mini LED Gaming Monitör. 144Hz, HDR-1400, G-SYNC Ultimate.",
            Price = 89999.99m,
            StockQuantity = 3,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1547394765-185e1e68f34e?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Corsair K100 RGB",
            Description = "Corsair K100 RGB Mekanik Gaming Klavye. Cherry MX Speed anahtarlar, iCUE RGB lighting.",
            Price = 7299.99m,
            StockQuantity = 17,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1595044426077-d36d9236d54a?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
   
     
        new Product
        {
            Name = "Samsung 980 PRO 2TB NVMe SSD",
            Description = "Samsung 980 PRO 2TB NVMe SSD. PCIe 4.0, 7000MB/s okuma hızı, 5 yıl garanti.",
            Price = 6799.99m,
            StockQuantity = 28,
            Category = "Depolama",
            ImageUrl = "https://images.unsplash.com/photo-1591488320449-011701bb6704?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Xiaomi Mi Band 8",
            Description = "Xiaomi Mi Band 8 Akıllı Bileklik. 1.62 inç AMOLED ekran, 16 gün pil ömrü, 150+ spor modu.",
            Price = 899.99m,
            StockQuantity = 156,
            Category = "Giyilebilir",
            ImageUrl = "https://images.unsplash.com/photo-1544117519-31a4b719223d?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Fitbit Versa 4",
            Description = "Fitbit Versa 4 Sağlık ve Fitness Akıllı Saati. GPS, 6+ günlük pil ömrü, 40+ egzersiz modu.",
            Price = 6999.99m,
            StockQuantity = 34,
            Category = "Giyilebilir",
            ImageUrl = "https://images.unsplash.com/photo-1575311373937-040b8e1fd5b6?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "OnePlus 12",
            Description = "OnePlus 12 Flagship Smartphone. Snapdragon 8 Gen 3, 16GB RAM, 512GB depolama, 100W hızlı şarj.",
            Price = 42999.99m,
            StockQuantity = 19,
            Category = "Telefon",
            ImageUrl = "https://images.unsplash.com/photo-1512054502232-10a0a035d672?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Google Pixel 8 Pro",
            Description = "Google Pixel 8 Pro. Tensor G3 çip, 12GB RAM, 256GB depolama, AI destekli kamera.",
            Price = 39999.99m,
            StockQuantity = 16,
            Category = "Telefon",
            ImageUrl = "https://images.unsplash.com/photo-1598300042247-d088f8ab3a91?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Microsoft Surface Pro 9",
            Description = "Microsoft Surface Pro 9 2-in-1 Tablet. Intel Core i7, 16GB RAM, 512GB SSD, detachable klavye.",
            Price = 56999.99m,
            StockQuantity = 11,
            Category = "Tablet",
            ImageUrl = "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "Lenovo ThinkPad X1 Carbon Gen 11",
            Description = "Lenovo ThinkPad X1 Carbon Gen 11. Intel Core i7-1365U, 32GB RAM, 1TB SSD, 14 inç 2.8K ekran.",
            Price = 67999.99m,
            StockQuantity = 8,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        },
        new Product
        {
            Name = "HP Spectre x360 16",
            Description = "HP Spectre x360 16 inç Convertible Laptop. Intel Core i7, 16GB RAM, 1TB SSD, OLED dokunmatik ekran.",
            Price = 54999.99m,
            StockQuantity = 13,
            Category = "Bilgisayar",
            ImageUrl = "https://images.unsplash.com/photo-1484788984921-03950022c9ef?w=500&auto=format&fit=crop&q=60",
            CreatedAt = DateTime.UtcNow
        }
    };

            _context.Products.AddRange(sampleProducts);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created {sampleProducts.Count} sample products with various categories and real image URLs");
        }

    }
}