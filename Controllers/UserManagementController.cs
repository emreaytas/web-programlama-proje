using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webprogbackend.Attributes;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Models.Enums;

namespace webprogbackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers(
            [FromQuery] string searchTerm = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Username.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Total-Pages", totalPages.ToString());
            Response.Headers.Add("X-Current-Page", page.ToString());

            return users;
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Cart)
                .ThenInclude(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var userDetails = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.UpdatedAt,
                OrderCount = user.Orders.Count,
                TotalSpent = user.Orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount),
                CartItemsCount = user.Cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0,
                RecentOrders = user.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        o.TotalAmount,
                        o.Status,
                        o.CreatedAt
                    })
            };

            return userDetails;
        }

        // GET: api/Users/Profile
        [HttpGet("Profile")]
        public async Task<ActionResult<object>> GetCurrentUserProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Cart)
                .ThenInclude(c => c.CartItems)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var profile = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                OrderCount = user.Orders.Count,
                TotalSpent = user.Orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount),
                CartItemsCount = user.Cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0
            };

            return profile;
        }

        // PUT: api/Users/Profile
        [HttpPut("Profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if username is already taken by another user
            if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != userId))
            {
                return BadRequest("Username is already taken");
            }

            // Check if email is already taken by another user
            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId))
            {
                return BadRequest("Email is already taken");
            }

            user.Username = model.Username;
            user.Email = model.Email;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Users/5/Role
        [HttpPut("{id}/Role")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleModel model)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Role = model.Role;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }



        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user has orders
            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
            if (hasOrders)
            {
                return BadRequest("Cannot delete user with existing orders");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Users/Stats
        [HttpGet("Stats")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<object>> GetUserStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var adminUsers = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
            var regularUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User);
            var moderatorUsers = await _context.Users.CountAsync(u => u.Role == UserRole.Moderator);

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= thisMonth);
            var newUsersLastMonth = await _context.Users.CountAsync(u => u.CreatedAt >= lastMonth && u.CreatedAt < thisMonth);

            var topCustomers = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .Join(_context.Users,
                      tc => tc.UserId,
                      u => u.Id,
                      (tc, u) => new
                      {
                          u.Id,
                          u.Username,
                          u.Email,
                          TotalSpent = tc.TotalSpent,
                          OrderCount = tc.OrderCount
                      })
                .ToListAsync();

            var stats = new
            {
                TotalUsers = totalUsers,
                AdminUsers = adminUsers,
                RegularUsers = regularUsers,
                ModeratorUsers = moderatorUsers,
                NewUsersThisMonth = newUsersThisMonth,
                NewUsersLastMonth = newUsersLastMonth,
                UserGrowth = newUsersLastMonth > 0
                    ? ((newUsersThisMonth - newUsersLastMonth) / (double)newUsersLastMonth) * 100
                    : 0,
                TopCustomers = topCustomers
            };

            return stats;
        }
    }

    // DTO Models
    public class UpdateProfileModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class UpdateRoleModel
    {
        public UserRole Role { get; set; }
    }


}