using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int TotalUsers { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalRequests { get; set; }
        public int TotalTrucks { get; set; }
        public List<ApplicationUser> RecentUsers { get; set; } = new();
        public List<WasteRequest> RecentRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Redirect to Dashboard
            return RedirectToPage("/Admin/Dashboard");
        }
    }
}

