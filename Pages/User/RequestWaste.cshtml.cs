using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Pages.User
{
    [Authorize]
    public class RequestWasteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestWasteModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool SubmittedSuccessfully { get; private set; }

        public class InputModel
        {
            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.StringLength(255)]
            public string Location { get; set; } = string.Empty;

            public string LocationName { get; set; } = string.Empty;

            public double? Latitude { get; set; }

            public double? Longitude { get; set; }

            [System.ComponentModel.DataAnnotations.Required]
            public string WasteType { get; set; } = string.Empty;

            public DateTime? PreferredDateTime { get; set; }

            [System.ComponentModel.DataAnnotations.StringLength(500)]
            public string? Notes { get; set; }

            public IFormFile? Photo { get; set; }
        }

        public void OnGet()
        {
            ViewData["Title"] = "Request Waste Collection";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["Title"] = "Request Waste Collection";

            var allowedTypes = new[] { "Plastic", "Organic", "Paper", "Metal", "E-waste", "Mixed" };
            if (!allowedTypes.Contains(Input.WasteType))
            {
                ModelState.AddModelError(nameof(Input.WasteType), "Please select a valid waste type.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            string? savedPhotoPath = null;
            if (Input.Photo != null && Input.Photo.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }
                var fileName = $"waste_{Guid.NewGuid():N}{Path.GetExtension(Input.Photo.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await Input.Photo.CopyToAsync(stream);
                }
                savedPhotoPath = $"/uploads/{fileName}";
            }

            var request = new WasteRequest
            {
                UserId = user.Id,
                Location = Input.Location,
                LocationName = Input.LocationName,
                Latitude = Input.Latitude,
                Longitude = Input.Longitude,
                WasteType = Input.WasteType,
                Status = "Pending",
                RequestDate = DateTime.Now,
                PreferredCollectionDateTime = Input.PreferredDateTime,
                Notes = Input.Notes,
                PhotoPath = savedPhotoPath
            };

            // If user used the map, ensure LocationName is set (fallback to Location if empty)
            if (request.Latitude.HasValue && string.IsNullOrEmpty(request.LocationName))
            {
                request.LocationName = request.Location;
            }

            _context.WasteRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToPage("/User/RequestSuccess", new { id = request.RequestID });
        }
    }
}
