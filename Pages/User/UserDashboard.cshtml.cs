using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Pages.User
{
    [Authorize(Roles = "User")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser CurrentUser { get; private set; } = null!;
        public int TotalRequests { get; private set; }
        public int PendingCount { get; private set; }
        public int InProgressCount { get; private set; }
        public int CompletedCount { get; private set; }
        public List<WasteRequest> RecentRequests { get; private set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "My Dashboard";

            CurrentUser = await _userManager.GetUserAsync(User);

            var baseQuery = _context.WasteRequests.Where(r => r.UserId == CurrentUser.Id);

            TotalRequests = await baseQuery.CountAsync();
            PendingCount = await baseQuery.CountAsync(r => r.Status == "Pending");
            InProgressCount = await baseQuery.CountAsync(r => r.Status == "Assigned" || r.Status == "In Progress");
            CompletedCount = await baseQuery.CountAsync(r => r.Status == "Collected" || r.Status == "Completed");

            RecentRequests = await baseQuery
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToListAsync();
        }

        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending" => "badge bg-warning text-dark",
                "Assigned" => "badge bg-orange",
                "In Progress" => "badge bg-info",
                "Collected" => "badge bg-success",
                "Completed" => "badge bg-success",
                "Failed" => "badge bg-danger",
                _ => "badge bg-secondary"
            };
        }
    }
}
