using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webprogbackend.Attributes;
using webprogbackend.Data;
using webprogbackend.Models.Enums;

namespace webprogbackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/User
        [HttpGet("User")]
        public async Task<ActionResult<object>> GetUserDashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var userOrders = await _context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var userCart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var dashboard = new
            {
                TotalOrders = userOrders.Count,
                PendingOrders = userOrders.Count(o => o.Status == "Pending"),
                CompletedOrders = userOrders.Count(o => o.Status == "Delivered"),
                TotalSpent = userOrders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount),
                CartItemsCount = userCart?.CartItems?.Sum(ci => ci.Quantity) ?? 0,
                CartValue = userCart?.CartItems?.Sum(ci => ci.Product.Price * ci.Quantity) ?? 0,
                RecentOrders = userOrders
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
                    .ToList()
            };

            return dashboard;
        }

        // GET: api/Dashboard/Admin
        [HttpGet("Admin")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<object>> GetAdminDashboard()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            // Product statistics
            var totalProducts = await _context.Products.CountAsync();
            var lowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity <= 10 && p.StockQuantity > 0);
            var outOfStockProducts = await _context.Products.CountAsync(p => p.StockQuantity == 0);

            // Order statistics
            var totalOrders = await _context.Orders.CountAsync();
            var todayOrders = await _context.Orders.CountAsync(o => o.CreatedAt.Date == today);
            var thisMonthOrders = await _context.Orders.CountAsync(o => o.CreatedAt >= thisMonth);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");

            // Revenue statistics
            var totalRevenue = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            var todayRevenue = await _context.Orders
                .Where(o => o.CreatedAt.Date == today && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            var thisMonthRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= thisMonth && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            var lastMonthRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            // User statistics
            var totalUsers = await _context.Users.CountAsync();
            var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= thisMonth);

            // Top selling products
            var topSellingProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .Join(_context.Products,
                      tp => tp.ProductId,
                      p => p.Id,
                      (tp, p) => new
                      {
                          ProductName = p.Name,
                          TotalSold = tp.TotalSold,
                          CurrentStock = p.StockQuantity
                      })
                .ToListAsync();

            // Recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    CustomerName = o.User.Username,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt
                })
                .ToListAsync();

            // Monthly sales data for chart
            var monthlySales = await _context.Orders
                .Where(o => o.CreatedAt >= DateTime.Today.AddMonths(-12) && o.Status != "Cancelled")
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Category statistics
            var categoryStats = await _context.Products
                .GroupBy(p => p.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    ProductCount = g.Count(),
                    TotalValue = g.Sum(p => p.Price * p.StockQuantity),
                    AveragePrice = g.Average(p => p.Price)
                })
                .ToListAsync();

            var dashboard = new
            {
                // Product Stats
                ProductStats = new
                {
                    TotalProducts = totalProducts,
                    LowStockProducts = lowStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    InStockProducts = totalProducts - outOfStockProducts
                },

                // Order Stats
                OrderStats = new
                {
                    TotalOrders = totalOrders,
                    TodayOrders = todayOrders,
                    ThisMonthOrders = thisMonthOrders,
                    PendingOrders = pendingOrders
                },

                // Revenue Stats
                RevenueStats = new
                {
                    TotalRevenue = totalRevenue,
                    TodayRevenue = todayRevenue,
                    ThisMonthRevenue = thisMonthRevenue,
                    LastMonthRevenue = lastMonthRevenue,
                    MonthlyGrowth = lastMonthRevenue > 0
                        ? ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
                        : 0
                },

                // User Stats
                UserStats = new
                {
                    TotalUsers = totalUsers,
                    NewUsersThisMonth = newUsersThisMonth
                },

                // Charts and Lists
                TopSellingProducts = topSellingProducts,
                RecentOrders = recentOrders,
                MonthlySales = monthlySales,
                CategoryStats = categoryStats
            };

            return dashboard;
        }

        // GET: api/Dashboard/Sales
        [HttpGet("Sales")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<object>> GetSalesDashboard(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var salesData = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status != "Cancelled")
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var topCustomers = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status != "Cancelled")
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
                          CustomerName = u.Username,
                          CustomerEmail = u.Email,
                          TotalSpent = tc.TotalSpent,
                          OrderCount = tc.OrderCount
                      })
                .ToListAsync();

            var result = new
            {
                DateRange = new { StartDate = startDate, EndDate = endDate },
                TotalSales = salesData.Sum(s => s.TotalSales),
                TotalOrders = salesData.Sum(s => s.OrderCount),
                AverageOrderValue = salesData.Any()
                    ? salesData.Sum(s => s.TotalSales) / salesData.Sum(s => s.OrderCount)
                    : 0,
                DailySales = salesData,
                TopCustomers = topCustomers
            };

            return result;
        }
    }
}