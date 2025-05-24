using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webprogbackend.Data;
using webprogbackend.Models;

namespace webprogbackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Cart
        [HttpGet]
        public async Task<ActionResult<Cart>> GetCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // GET: api/Cart/Summary
        [HttpGet("Summary")]
        public async Task<ActionResult<object>> GetCartSummary()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return new
                {
                    TotalItems = 0,
                    TotalAmount = 0m,
                    Items = new List<object>()
                };
            }

            var summary = new
            {
                TotalItems = cart.CartItems.Sum(ci => ci.Quantity),
                TotalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                Items = cart.CartItems.Select(ci => new
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    TotalPrice = ci.Product.Price * ci.Quantity
                })
            };

            return summary;
        }

        // POST: api/Cart/AddItem
        [HttpPost("AddItem")]
        public async Task<IActionResult> AddToCart([FromBody] CartItemModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound("Product not found");

            if (product.StockQuantity < model.Quantity)
                return BadRequest("Not enough stock available");

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
            if (cartItem != null)
            {
                cartItem.Quantity += model.Quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = model.ProductId,
                    Quantity = model.Quantity
                };
                cart.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/Cart/UpdateItem
        [HttpPut("UpdateItem")]
        public async Task<IActionResult> UpdateCartItem([FromBody] CartItemModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
            if (cartItem == null)
                return NotFound("Item not found in cart");

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product.StockQuantity < model.Quantity)
                return BadRequest("Not enough stock available");

            cartItem.Quantity = model.Quantity;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Cart/RemoveItem/{productId}
        [HttpDelete("RemoveItem/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null)
                return NotFound("Item not found in cart");

            cart.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Cart/Clear
        [HttpDelete("Clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            cart.CartItems.Clear();
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: api/Cart/Checkout
        [HttpGet("Checkout")]
        public async Task<ActionResult<object>> GetCheckoutInfo()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest("Cart is empty");

            var checkoutInfo = new
            {
                TotalItems = cart.CartItems.Sum(ci => ci.Quantity),
                TotalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                Items = cart.CartItems.Select(ci => new
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    TotalPrice = ci.Product.Price * ci.Quantity,
                    AvailableStock = ci.Product.StockQuantity
                })
            };

            return checkoutInfo;
        }
    }

    public class CartItemModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
} 