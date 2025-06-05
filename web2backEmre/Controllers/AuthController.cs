using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Models.Enums;
using webprogbackend.Services;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanýcý kayýt iþlemi
        /// </summary>
        [HttpPost("Register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Geçersiz veri giriþi",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Email kontrolü
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUserByEmail != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu email adresi zaten kullanýlýyor"
                    });
                }

                // Username kontrolü
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

                if (existingUserByUsername != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu kullanýcý adý zaten kullanýlýyor"
                    });
                }

                // Þifre kontrolü
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Þifreler eþleþmiyor"
                    });
                }

                // Yeni kullanýcý oluþtur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.User, // Varsayýlan olarak User rolü
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Kullanýcý için sepet oluþtur
                var cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // JWT token oluþtur
                var token = _jwtService.GenerateToken(user);

                // Hoþ geldin emaili gönder (arka planda)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Welcome email could not be sent to {Email}", user.Email);
                }

                _logger.LogInformation("New user registered: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Kayýt baþarýyla tamamlandý",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Kayýt sýrasýnda bir hata oluþtu. Lütfen tekrar deneyin."
                });
            }
        }

        [HttpPost("Register-Admin")]
        public async Task<ActionResult<AuthResponse>> RegisterAdmin([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Geçersiz veri giriþi",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Email kontrolü
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUserByEmail != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu email adresi zaten kullanýlýyor"
                    });
                }

                // Username kontrolü
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

                if (existingUserByUsername != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu kullanýcý adý zaten kullanýlýyor"
                    });
                }

                // Þifre kontrolü
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Þifreler eþleþmiyor"
                    });
                }

                // Yeni kullanýcý oluþtur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Kullanýcý için sepet oluþtur
                var cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // JWT token oluþtur
                var token = _jwtService.GenerateToken(user);

                // Hoþ geldin emaili gönder (arka planda)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Welcome email could not be sent to {Email}", user.Email);
                }

                _logger.LogInformation("New user registered: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Kayýt baþarýyla tamamlandý",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Kayýt sýrasýnda bir hata oluþtu. Lütfen tekrar deneyin."
                });
            }
        }



        /// <summary>
        /// Kullanýcý giriþ iþlemi
        /// </summary>
        [HttpPost("Login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Geçersiz veri giriþi",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Kullanýcýyý email ile bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Email veya þifre hatalý"
                    });
                }

                // Þifre kontrolü
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Email veya þifre hatalý"
                    });
                }

                // JWT token oluþtur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("User logged in: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Giriþ baþarýlý",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Giriþ sýrasýnda bir hata oluþtu. Lütfen tekrar deneyin."
                });
            }
        }

        /// <summary>
        /// Mevcut kullanýcý bilgilerini getir
        /// </summary>
        [HttpGet("Me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kullanýcý bulunamadý" });
                }

                return Ok(new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "Kullanýcý bilgileri alýnýrken hata oluþtu" });
            }
        }

        /// <summary>
        /// Kullanýcý rolünü getir
        /// </summary>
        [HttpGet("MyRole")]
        [Authorize]
        public ActionResult<object> GetMyRole()
        {
            try
            {
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ??
                               User.FindFirst("Role")?.Value;

                return Ok(new
                {
                    Role = roleClaim,
                    IsAdmin = roleClaim == UserRole.Admin.ToString(),
                    IsUser = roleClaim == UserRole.User.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role");
                return StatusCode(500, new { message = "Rol bilgisi alýnýrken hata oluþtu" });
            }
        }

        /// <summary>
        /// Þifre deðiþtirme
        /// </summary>
        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kullanýcý bulunamadý" });
                }

                // Mevcut þifre kontrolü
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                {
                    return BadRequest(new { message = "Mevcut þifre hatalý" });
                }

                // Yeni þifre kontrolü
                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    return BadRequest(new { message = "Yeni þifreler eþleþmiyor" });
                }

                // Þifreyi güncelle
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed for user: {Email}", user.Email);

                return Ok(new { message = "Þifre baþarýyla deðiþtirildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { message = "Þifre deðiþtirme sýrasýnda hata oluþtu" });
            }
        }

        /// <summary>
        /// Token doðrulama
        /// </summary>
        [HttpPost("ValidateToken")]
        public ActionResult ValidateToken([FromBody] TokenValidationRequest request)
        {
            try
            {
                var isValid = _jwtService.IsTokenValid(request.Token);

                return Ok(new
                {
                    IsValid = isValid,
                    Message = isValid ? "Token geçerli" : "Token geçersiz"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return Ok(new
                {
                    IsValid = false,
                    Message = "Token doðrulama sýrasýnda hata oluþtu"
                });
            }
        }

        /// <summary>
        /// Çýkýþ iþlemi (client-side token temizleme)
        /// </summary>
        [HttpPost("Logout")]
        [Authorize]
        public ActionResult Logout()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                _logger.LogInformation("User logged out: {Email}", userEmail);

                return Ok(new { message = "Çýkýþ baþarýlý" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return Ok(new { message = "Çýkýþ iþlemi tamamlandý" });
            }
        }

        /// <summary>
        /// Hesap silme
        /// </summary>
        [HttpDelete("DeleteAccount")]
        [Authorize]
        public async Task<ActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kullanýcý bulunamadý" });
                }

                // Þifre kontrolü
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new { message = "Þifre hatalý" });
                }

                // Ýliþkili verileri sil (cascade delete çalýþacak)
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User account deleted: {Email}", user.Email);

                return Ok(new { message = "Hesap baþarýyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account");
                return StatusCode(500, new { message = "Hesap silme sýrasýnda hata oluþtu" });
            }
        }
    }

    #region Request/Response Models

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Kullanýcý adý gereklidir")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanýcý adý 3-50 karakter arasýnda olmalýdýr")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Þifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Þifre tekrarý gereklidir")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Þifre gereklidir")]
        public string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mevcut þifre gereklidir")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni þifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Yeni þifre tekrarý gereklidir")]
        public string ConfirmNewPassword { get; set; }
    }

    public class TokenValidationRequest
    {
        [Required]
        public string Token { get; set; }
    }

    public class DeleteAccountRequest
    {
        [Required(ErrorMessage = "Þifre gereklidir")]
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    #endregion
}