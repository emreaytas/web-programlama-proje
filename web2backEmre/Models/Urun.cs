using System.ComponentModel.DataAnnotations;

namespace webprogbackend.Models
{
    public class Urun
    {
        public int Id { get; set; }

        public int UserId { get; set; } = 0;

        public int UrunId { get; set; } = 0;

        public DateTime EklenmeTarihi { get; set; } = DateTime.UtcNow;

    }
}