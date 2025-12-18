using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;
using WasteCollectionSystem.Services;
using System.ComponentModel.DataAnnotations;

namespace WasteCollectionSystem.Pages
{
    public class GuestRequestModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly CartService _cartService;

        public GuestRequestModel(ApplicationDbContext context, NotificationService notificationService, CartService cartService)
        {
            _context = context;
            _notificationService = notificationService;
            _cartService = cartService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your name")]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please enter your phone number")]
            [Phone]
            [StringLength(20)]
            public string Phone { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please provide a location")]
            [StringLength(255)]
            public string Location { get; set; } = string.Empty;

            public string LocationName { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }

            [Required(ErrorMessage = "Please select a waste type")]
            public string WasteType { get; set; } = string.Empty;

            [StringLength(500)]
            public string? Notes { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new WasteRequest
            {
                UserId = null, // Guest request
                GuestName = Input.Name,
                GuestPhone = Input.Phone,
                Location = Input.Location,
                LocationName = string.IsNullOrEmpty(Input.LocationName) ? Input.Location : Input.LocationName,
                Latitude = Input.Latitude,
                Longitude = Input.Longitude,
                WasteType = Input.WasteType,
                Status = "Pending",
                RequestDate = DateTime.Now,
                Notes = Input.Notes
            };

            _context.WasteRequests.Add(request);
            await _context.SaveChangesAsync();

            // Add to guest cart
            await _cartService.AddToCartAsync(request.RequestID);

            TempData["CartToast"] = "true";
            TempData["SuccessMessage"] = $"Request submitted successfully! Click the cart icon to proceed to payment.";
            return RedirectToPage("/Index");
        }
    }
}
