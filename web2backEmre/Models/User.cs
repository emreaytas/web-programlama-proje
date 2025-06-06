using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using webprogbackend.Models.Enums;

namespace webprogbackend.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanýcý adý zorunludur")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanýcý adý 3-50 karakter arasýnda olmalýdýr")]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Þifre zorunludur")]
        [JsonIgnore] // API yanýtlarýnda þifreyi gizle
        public string Password { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        // Helper methods
        public bool IsAdmin() => Role == UserRole.Admin;
        public bool IsUser() => Role == UserRole.User;
    }
}
