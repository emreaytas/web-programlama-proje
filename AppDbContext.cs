using MakaleAPI.Models.Gereksiz;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MakaleAPI.Models
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<LogEntry> LogEntries { get; set; } // loglar için tuttuğumuz tablodur. mis gibi çalışır.
        // public DbSet<EncryptedImageData> EncryptedImageData { get; set; }
        public DbSet<Makale> Makaleler { get; set; }
        public DbSet<MakaleWEmail> MakaleEmail { get; set; }
        public DbSet<MakaleFF> MakaleFF { get; set; }

        public DbSet<MakaleAA> MakaleAA { get; set; } // kullanılmıyor boş geçebiliriz... 
        public DbSet<MakaleBB> MakaleBB { get; set; } // hiç ellenmemiş makeleleri tutarız. her şeyiyle düzgün çalışıyor. MakaleBBController ile çalışıyor. 
        public DbSet<MakaleCC> MakaleCC { get; set; } // MakaleceCC'yi tuttuğumuz tablodur. burada resimlerin bulanık olup olmadığını tutarız. MakaleCCController ile çalışır.  
        public DbSet<MakaleDD> MakaleDD { get; set; } // tamamen işlenmiş pdfleri burada tutacağız. TamISlemController ile çalışır. 
        public DbSet<MakaleEE> MakaleEE { get; set; } // anonize edilmiş ve hakem yorumu eklenmiş geçerli mi değil mi belirtilmiş makaleleri tutarız.
        public DbSet<MakaleGG> MakaleGG { get; set; } // orjinal + hakem yorumlu olan pdf olacak bu. 



        public DbSet<MakaleDDAES> MakaleDDAES { get; set; }
        public DbSet<MakaleDDAES2> MakaleDDAES2 { get; set; }

        public DbSet<Hakem> Hakem { get; set; } // hakemlerin bilgilerini tuttuğumuz tablodur. hakemlerin alanlarını ve takip numaralarını tutarız. böylece hakemin alanı ile  
        public DbSet<Alan> Alanlar { get; set; } // alanların bilgilerini tuttuğumuz tablodur.
        public DbSet<MakaleAlan> MakaleAlanlar { get; set; } // makalelerin alanlarını tuttuğumuz tablodur. makaleye alan ataması yaparız takip numarası ile. ve alanın id'si ile.

        public DbSet<YorumluMakale> YorumluMakales { get; set; }
        public DbSet<MakaleLog> MakaleLoglari { get; set; }
        public DbSet<MakaleMesaj> MakaleMesajlari { get; set; }
        public DbSet<Mesajlar> Mesajlar { get; set; }
        public DbSet<Mesajlar2> EditorMesajlar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MakaleBB için konfigürasyon
            modelBuilder.Entity<MakaleBB>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<MakaleBB>()
                .Property(m => m.Email)
                .IsRequired();

            modelBuilder.Entity<MakaleBB>()
                .Property(m => m.TakipNumarasi)
                .IsRequired();

            // MakaleCC için konfigürasyon
            modelBuilder.Entity<MakaleCC>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<MakaleCC>()
                .Property(m => m.Email)
                .IsRequired();

            modelBuilder.Entity<MakaleCC>()
                .Property(m => m.TakipNumarasi)
                .IsRequired();

            modelBuilder.Entity<MakaleCC>()
                .Property(m => m.IslemTarihi)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<MakaleCC>()
                .Property(m => m.ResimlerBulanik)
                .IsRequired()
                .HasDefaultValue(false);

            // Hakem ayarları
            modelBuilder.Entity<Hakem>()
                .Property(h => h.Name)
                .IsRequired();

            // Hakem.TakipNolari listesinin JSON olarak saklanmasını sağla - Daha güvenli serializasyon
            modelBuilder.Entity<Hakem>()
                .Property(h => h.TakipNolari)
                .HasConversion(
                    // Null kontrolü ekle ve boş liste yerine [] serialization yap
                    v => v != null && v.Count > 0 ? JsonSerializer.Serialize(v, new JsonSerializerOptions()) : "[]",
                    // Deserializasyon sırasında null kontrolü
                    v => string.IsNullOrEmpty(v) ? new List<string>() :
                         JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>());

            // Hakem.Alanlar listesinin JSON olarak saklanmasını sağla - Daha güvenli serializasyon
            modelBuilder.Entity<Hakem>()
                .Property(h => h.Alanlar)
                .HasConversion(
                    // Null kontrolü ekle ve boş liste yerine [] serialization yap
                    v => v != null && v.Count > 0 ? JsonSerializer.Serialize(v, new JsonSerializerOptions()) : "[]",
                    // Deserializasyon sırasında null kontrolü 
                    v => string.IsNullOrEmpty(v) ? new List<int>() :
                         JsonSerializer.Deserialize<List<int>>(v, new JsonSerializerOptions()) ?? new List<int>());

            // Alan için konfigürasyon
            modelBuilder.Entity<Alan>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<Alan>()
                .Property(a => a.Name)
                .IsRequired();

            // MakaleAlan için konfigürasyon
            modelBuilder.Entity<MakaleAlan>()
                .HasKey(ma => ma.Id);

            modelBuilder.Entity<MakaleAlan>()
                .Property(ma => ma.TakipNumarasi)
                .IsRequired();

            modelBuilder.Entity<MakaleAlan>()
                .HasOne(ma => ma.Alan)
                .WithMany()
                .HasForeignKey(ma => ma.AlanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Data - Alan verileri
            SeedAreas(modelBuilder);
        }

        private void SeedAreas(ModelBuilder modelBuilder)
        {
            // Alan verileri (önceki ile aynı)
            var alanlar = new List<Alan>
            {
                // Yapay Zeka ve Makine Öğrenimi
                new Alan { Id = 1, Name = "Derin Öğrenme", AnaKategori = "Yapay Zeka ve Makine Öğrenimi", AltKategori = "Derin Öğrenme" },
                new Alan { Id = 2, Name = "Doğal Dil İşleme", AnaKategori = "Yapay Zeka ve Makine Öğrenimi", AltKategori = "Doğal Dil İşleme" },
                new Alan { Id = 3, Name = "Bilgisayarla Görü", AnaKategori = "Yapay Zeka ve Makine Öğrenimi", AltKategori = "Bilgisayarla Görü" },
                new Alan { Id = 4, Name = "Generatif Yapay Zeka", AnaKategori = "Yapay Zeka ve Makine Öğrenimi", AltKategori = "Generatif Yapay Zeka" },
                
                // İnsan-Bilgisayar Etkileşimi
                new Alan { Id = 5, Name = "Beyin-Bilgisayar Arayüzleri", AnaKategori = "İnsan-Bilgisayar Etkileşimi", AltKategori = "Beyin-Bilgisayar Arayüzleri" },
                new Alan { Id = 6, Name = "Kullanıcı Deneyimi Tasarımı", AnaKategori = "İnsan-Bilgisayar Etkileşimi", AltKategori = "Kullanıcı Deneyimi Tasarımı" },
                new Alan { Id = 7, Name = "Artırılmış ve Sanal Gerçeklik", AnaKategori = "İnsan-Bilgisayar Etkileşimi", AltKategori = "Artırılmış ve Sanal Gerçeklik" },
                
                // Büyük Veri ve Veri Analitiği
                new Alan { Id = 8, Name = "Veri Madenciliği", AnaKategori = "Büyük Veri ve Veri Analitiği", AltKategori = "Veri Madenciliği" },
                new Alan { Id = 9, Name = "Veri Görselleştirme", AnaKategori = "Büyük Veri ve Veri Analitiği", AltKategori = "Veri Görselleştirme" },
                new Alan { Id = 10, Name = "Veri İşleme Sistemleri", AnaKategori = "Büyük Veri ve Veri Analitiği", AltKategori = "Veri İşleme Sistemleri" },
                new Alan { Id = 11, Name = "Zaman Serisi Analizi", AnaKategori = "Büyük Veri ve Veri Analitiği", AltKategori = "Zaman Serisi Analizi" },
                
                // Siber Güvenlik
                new Alan { Id = 12, Name = "Şifreleme Algoritmaları", AnaKategori = "Siber Güvenlik", AltKategori = "Şifreleme Algoritmaları" },
                new Alan { Id = 13, Name = "Güvenli Yazılım Geliştirme", AnaKategori = "Siber Güvenlik", AltKategori = "Güvenli Yazılım Geliştirme" },
                new Alan { Id = 14, Name = "Ağ Güvenliği", AnaKategori = "Siber Güvenlik", AltKategori = "Ağ Güvenliği" },
                new Alan { Id = 15, Name = "Kimlik Doğrulama Sistemleri", AnaKategori = "Siber Güvenlik", AltKategori = "Kimlik Doğrulama Sistemleri" },
                new Alan { Id = 16, Name = "Adli Bilişim", AnaKategori = "Siber Güvenlik", AltKategori = "Adli Bilişim" },
                
                // Ağ ve Dağıtık Sistemler
                new Alan { Id = 17, Name = "5G ve Yeni Nesil Ağlar", AnaKategori = "Ağ ve Dağıtık Sistemler", AltKategori = "5G ve Yeni Nesil Ağlar" },
                new Alan { Id = 18, Name = "Bulut Bilişim", AnaKategori = "Ağ ve Dağıtık Sistemler", AltKategori = "Bulut Bilişim" },
                new Alan { Id = 19, Name = "Blockchain Teknolojisi", AnaKategori = "Ağ ve Dağıtık Sistemler", AltKategori = "Blockchain Teknolojisi" },
                new Alan { Id = 20, Name = "P2P ve Merkeziyetsiz Sistemler", AnaKategori = "Ağ ve Dağıtık Sistemler", AltKategori = "P2P ve Merkeziyetsiz Sistemler" }
            };

            modelBuilder.Entity<Alan>().HasData(alanlar);
        }
    }
}