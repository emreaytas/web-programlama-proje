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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanýcý kaydý - Varsayýlan olarak User rolü atanýr
        /// </summary>
        [HttpPost("Register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest(new { message = "Email already registered" });
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    return BadRequest(new { message = "Username already taken" });
                }

                // Create new user with User role by default
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = UserRole.User, // Varsayýlan rol
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create cart for user
                var cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation($"User registered successfully: {user.Email} with role: {user.Role}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    },
                    Message = "Registration successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration");
                return StatusCode(500, new { message = "Registration failed" });
            }
        }

        /// <summary>
        /// Admin kaydý - Sadece mevcut adminler tarafýndan kullanýlabilir
        /// </summary>
        [HttpPost("RegisterAdmin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AuthResponse>> RegisterAdmin([FromBody] RegisterAdminModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest(new { message = "Email already registered" });
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    return BadRequest(new { message = "Username already taken" });
                }

                // Create new admin user
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = model.Role, // Admin veya Moderator
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Admin için cart oluþturmaya gerek yok, ama istenirse eklenebilir
                if (model.CreateCart)
                {
                    var cart = new Cart
                    {
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var currentAdmin = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation($"Admin user created: {user.Email} with role: {user.Role} by {currentAdmin}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = null, // Admin kaydýnda token vermiyoruz
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    },
                    Message = $"{user.Role} user created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during admin registration");
                return StatusCode(500, new { message = "Admin registration failed" });
            }
        }

        /// <summary>
        /// Kullanýcý giriþi - Rol bilgisi token'da döner
        /// </summary>
        [HttpPost("Login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    return BadRequest(new { message = "Invalid email or password" });
                }

                // Generate JWT token with role information
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation($"User logged in: {user.Email} with role: {user.Role}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    },
                    Message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login");
                return StatusCode(500, new { message = "Login failed" });
            }
        }



        


        [HttpPost("Logout")]
        [Authorize]
        public ActionResult Logout()
        {
            return Ok(new { message = "Logout successful" });
        }

        [HttpPost("ValidateToken")]
        public ActionResult ValidateToken([FromBody] ValidateTokenModel model)
        {
            try
            {
                var isValid = _jwtService.IsTokenValid(model.Token);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token validation");
                return Ok(new { isValid = false });
            }
        }

        #region Private Methods

        private List<string> GetRolePermissions(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => new List<string>
                {
                    "user.manage",
                    "product.manage",
                    "order.manage",
                    "dashboard.admin",
                    "role.change",
                    "system.admin"
                },
                UserRole.Moderator => new List<string>
                {
                    "product.manage",
                    "order.view",
                    "user.view",
                    "dashboard.moderator"
                },
                UserRole.User => new List<string>
                {
                    "product.view",
                    "cart.manage",
                    "order.own",
                    "profile.manage"
                },
                _ => new List<string>()
            };
        }

        #endregion
    }

    #region DTO Models

    public class RegisterModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterAdminModel : RegisterModel
    {
        [Required]
        public UserRole Role { get; set; } = UserRole.Admin;

        public bool CreateCart { get; set; } = false;
    }

    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }
    }

    public class ChangeRoleModel
    {
        [Required]
        public UserRole NewRole { get; set; }

        public string? Reason { get; set; }
    }

    public class ValidateTokenModel
    {
        [Required]
        public string Token { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }
        public string Message { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }

    public class UserRoleInfo : UserInfo
    {
        public string RoleName { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }
        public bool IsUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion
}