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
    public class RequestSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestSuccessModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public WasteRequest CurrentRequest { get; private set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ViewData["Title"] = "Request Submitted";
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var r = await _context.WasteRequests.FirstOrDefaultAsync(x => x.RequestID == id && x.UserId == user.Id);
            if (r == null) return NotFound();
            CurrentRequest = r;
            return Page();
        }
    }
}
