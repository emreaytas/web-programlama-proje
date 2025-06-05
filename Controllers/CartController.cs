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
        public async Task<ActionResult<object>> GetCart()
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

            var cartResponse = new
            {
                cart.Id,
                cart.UserId,
                cart.CreatedAt,
                cart.UpdatedAt,
                Items = cart.CartItems.Select(ci => new
                {
                    ci.Id,
                    ci.ProductId,
                    ci.Quantity,
                    ci.CreatedAt,
                    Product = new
                    {
                        ci.Product.Id,
                        ci.Product.Name,
                        ci.Product.Description,
                        ci.Product.Price,
                        ci.Product.StockQuantity,
                        ci.Product.Category,
                        ci.Product.ImageUrl
                    },
                    TotalPrice = ci.Product.Price * ci.Quantity,
                    IsAvailable = ci.Product.StockQuantity >= ci.Quantity
                }),
                Summary = new
                {
                    TotalItems = cart.CartItems.Sum(ci => ci.Quantity),
                    TotalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                    ItemsCount = cart.CartItems.Count,
                    HasUnavailableItems = cart.CartItems.Any(ci => ci.Product.StockQuantity < ci.Quantity)
                }
            };

            return cartResponse;
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
                    ItemsCount = 0,
                    HasItems = false
                };
            }

            var summary = new
            {
                TotalItems = cart.CartItems.Sum(ci => ci.Quantity),
                TotalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                ItemsCount = cart.CartItems.Count,
                HasItems = cart.CartItems.Any(),
                HasUnavailableItems = cart.CartItems.Any(ci => ci.Product.StockQuantity < ci.Quantity)
            };

            return summary;
        }

        // POST: api/Cart/AddItem
        [HttpPost("AddItem")]
        public async Task<ActionResult<object>> AddToCart([FromBody] AddToCartModel model)
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
                await _context.SaveChangesAsync();
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound("Product not found");

            var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
            var newQuantity = existingCartItem?.Quantity + model.Quantity ?? model.Quantity;

            if (product.StockQuantity < newQuantity)
                return BadRequest(new
                {
                    Message = "Not enough stock available",
                    AvailableStock = product.StockQuantity,
                    RequestedQuantity = newQuantity
                });

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += model.Quantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = model.ProductId,
                    Quantity = model.Quantity
                };
                cart.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item added to cart successfully" });
        }

        // POST: api/Cart/AddMultiple
        [HttpPost("AddMultiple")]
        public async Task<ActionResult<object>> AddMultipleToCart([FromBody] List<AddToCartModel> models)
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
                await _context.SaveChangesAsync();
            }

            var productIds = models.Select(m => m.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var errors = new List<string>();
            var successCount = 0;

            foreach (var model in models)
            {
                var product = products.FirstOrDefault(p => p.Id == model.ProductId);
                if (product == null)
                {
                    errors.Add($"Product with ID {model.ProductId} not found");
                    continue;
                }

                var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
                var newQuantity = existingCartItem?.Quantity + model.Quantity ?? model.Quantity;

                if (product.StockQuantity < newQuantity)
                {
                    errors.Add($"Not enough stock for {product.Name}. Available: {product.StockQuantity}, Requested: {newQuantity}");
                    continue;
                }

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += model.Quantity;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = model.ProductId,
                        Quantity = model.Quantity
                    };
                    cart.CartItems.Add(cartItem);
                }

                successCount++;
            }

            if (successCount > 0)
            {
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                Message = $"{successCount} items added successfully",
                SuccessCount = successCount,
                Errors = errors
            });
        }

        // PUT: api/Cart/UpdateItem
        [HttpPut("UpdateItem")]
        public async Task<ActionResult<object>> UpdateCartItem([FromBody] UpdateCartItemModel model)
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
                return BadRequest(new
                {
                    Message = "Not enough stock available",
                    AvailableStock = product.StockQuantity,
                    RequestedQuantity = model.Quantity
                });

            cartItem.Quantity = model.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cart item updated successfully" });
        }

        // DELETE: api/Cart/RemoveItem/{productId}
        [HttpDelete("RemoveItem/{productId}")]
        public async Task<ActionResult<object>> RemoveFromCart(int productId)
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
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item removed from cart successfully" });
        }

        // DELETE: api/Cart/RemoveMultiple
        [HttpDelete("RemoveMultiple")]
        public async Task<ActionResult<object>> RemoveMultipleFromCart([FromBody] List<int> productIds)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            var itemsToRemove = cart.CartItems.Where(ci => productIds.Contains(ci.ProductId)).ToList();

            if (!itemsToRemove.Any())
                return NotFound("No matching items found in cart");

            foreach (var item in itemsToRemove)
            {
                cart.CartItems.Remove(item);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"{itemsToRemove.Count} items removed from cart successfully",
                RemovedCount = itemsToRemove.Count
            });
        }

        // DELETE: api/Cart/Clear
        [HttpDelete("Clear")]
        public async Task<ActionResult<object>> ClearCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            var itemCount = cart.CartItems.Count;
            cart.CartItems.Clear();
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Cart cleared successfully",
                RemovedItemsCount = itemCount
            });
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

            var unavailableItems = cart.CartItems
                .Where(ci => ci.Product.StockQuantity < ci.Quantity)
                .Select(ci => new
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    RequestedQuantity = ci.Quantity,
                    AvailableStock = ci.Product.StockQuantity
                })
                .ToList();

            var checkoutInfo = new
            {
                TotalItems = cart.CartItems.Sum(ci => ci.Quantity),
                TotalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                ItemsCount = cart.CartItems.Count,
                Items = cart.CartItems.Select(ci => new
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    TotalPrice = ci.Product.Price * ci.Quantity,
                    AvailableStock = ci.Product.StockQuantity,
                    IsAvailable = ci.Product.StockQuantity >= ci.Quantity
                }),
                HasUnavailableItems = unavailableItems.Any(),
                UnavailableItems = unavailableItems,
                CanProceedToCheckout = !unavailableItems.Any()
            };

            return checkoutInfo;
        }

        // PUT: api/Cart/Validate
        [HttpPut("Validate")]
        public async Task<ActionResult<object>> ValidateCartItems()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return Ok(new { Message = "Cart is empty", IsValid = true });

            var itemsToUpdate = new List<CartItem>();
            var itemsToRemove = new List<CartItem>();
            var validationResults = new List<object>();

            foreach (var item in cart.CartItems)
            {
                if (item.Product.StockQuantity == 0)
                {
                    itemsToRemove.Add(item);
                    validationResults.Add(new
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        Action = "Removed",
                        Reason = "Out of stock"
                    });
                }
                else if (item.Product.StockQuantity < item.Quantity)
                {
                    item.Quantity = item.Product.StockQuantity;
                    item.UpdatedAt = DateTime.UtcNow;
                    itemsToUpdate.Add(item);
                    validationResults.Add(new
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        Action = "Updated",
                        Reason = $"Quantity reduced to available stock: {item.Product.StockQuantity}"
                    });
                }
            }

            foreach (var item in itemsToRemove)
            {
                cart.CartItems.Remove(item);
            }

            if (itemsToUpdate.Any() || itemsToRemove.Any())
            {
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                Message = "Cart validation completed",
                IsValid = !validationResults.Any(),
                ValidationResults = validationResults,
                UpdatedItemsCount = itemsToUpdate.Count,
                RemovedItemsCount = itemsToRemove.Count
            });
        }
    }

    // DTO Models
    public class AddToCartModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}



