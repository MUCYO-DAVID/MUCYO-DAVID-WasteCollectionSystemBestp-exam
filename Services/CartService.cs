using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> GetCartCountAsync(string? userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // Logged-in user: count their WasteRequests
                return await _context.WasteRequests
                    .Where(r => r.UserId == userId && r.Status == "Pending")
                    .CountAsync();
            }
            else
            {
                // Guest: count items in their cart
                var sessionId = GetOrCreateSessionId();
                var cart = await _context.GuestCarts
                    .Include(gc => gc.Items)
                    .ThenInclude(i => i.WasteRequest) // Need to include WasteRequest to check status
                    .FirstOrDefaultAsync(gc => gc.SessionId == sessionId);
                
                return cart?.Items.Count(i => i.WasteRequest?.Status == "Pending") ?? 0;
            }
        }

        public async Task<List<WasteRequest>> GetCartItemsAsync(string? userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // Logged-in user: get their WasteRequests
                return await _context.WasteRequests
                    .Where(r => r.UserId == userId && r.Status == "Pending")
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
            }
            else
            {
                // Guest: get items from their cart
                var sessionId = GetOrCreateSessionId();
                var cart = await _context.GuestCarts
                    .Include(gc => gc.Items)
                        .ThenInclude(gci => gci.WasteRequest)
                    .FirstOrDefaultAsync(gc => gc.SessionId == sessionId);
                
                return cart?.Items
                    .Select(gci => gci.WasteRequest)
                    .Where(r => r != null && r.Status == "Pending")
                    .OrderByDescending(r => r.RequestDate)
                    .ToList() ?? new List<WasteRequest>();
            }
        }

        public async Task AddToCartAsync(int requestId)
        {
            var sessionId = GetOrCreateSessionId();
            
            // Get or create guest cart
            var cart = await _context.GuestCarts
                .FirstOrDefaultAsync(gc => gc.SessionId == sessionId);
            
            if (cart == null)
            {
                cart = new GuestCart
                {
                    SessionId = sessionId,
                    CreatedAt = DateTime.Now
                };
                _context.GuestCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Add item to cart if not already there
            var existingItem = await _context.GuestCartItems
                .FirstOrDefaultAsync(gci => gci.GuestCartId == cart.Id && gci.WasteRequestId == requestId);
            
            if (existingItem == null)
            {
                var cartItem = new GuestCartItem
                {
                    GuestCartId = cart.Id,
                    WasteRequestId = requestId,
                    AddedAt = DateTime.Now
                };
                _context.GuestCartItems.Add(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        private string GetOrCreateSessionId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return Guid.NewGuid().ToString();

            const string sessionKey = "GuestCartSessionId";
            
            if (context.Request.Cookies.TryGetValue(sessionKey, out var sessionId))
            {
                return sessionId;
            }

            // Create new session ID
            sessionId = Guid.NewGuid().ToString();
            context.Response.Cookies.Append(sessionKey, sessionId, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });

            return sessionId;
        }
    }
}
