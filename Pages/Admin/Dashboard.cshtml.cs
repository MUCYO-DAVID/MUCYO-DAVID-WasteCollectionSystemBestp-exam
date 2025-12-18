using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;
using WasteCollectionSystem.Services;

namespace WasteCollectionSystem.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public ApplicationUser CurrentAdmin { get; private set; } = null!;

        public int TotalUsers { get; private set; }
        public int DriversCount { get; private set; }
        public int TrucksCount { get; private set; }
        public int PendingRequests { get; private set; }
        public int TodaysCollections { get; private set; }
        public decimal TotalRevenue { get; private set; }

        public List<WasteRequest> RecentRequests { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Admin Panel";

            CurrentAdmin = await _userManager.GetUserAsync(User);
            if (CurrentAdmin == null)
            {
                return Challenge();
            }

            TotalUsers = await _userManager.Users.CountAsync();
            DriversCount = await _userManager.Users.CountAsync(u => u.Role == "Driver");
            TrucksCount = await _context.Trucks.CountAsync();
            PendingRequests = await _context.WasteRequests.CountAsync(r => r.Status == "Pending");
            TodaysCollections = await _context.Assignments.CountAsync(a => a.CompletionDate != null && a.CompletionDate.Value.Date == DateTime.Today);
            TotalRevenue = await _context.Payments.Where(p => p.PaymentStatus == "Paid").SumAsync(p => (decimal?)p.Amount) ?? 0;

            RecentRequests = await _context.WasteRequests
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestDate)
                .Take(10)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAssignAsync(int requestId)
        {
            var req = await _context.WasteRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestID == requestId);
            if (req == null) { TempData["ErrorMessage"] = "Request not found."; return RedirectToPage(); }

            var truck = await _context.Trucks
                .Include(t => t.Driver)
                .OrderBy(t => t.TruckID)
                .FirstOrDefaultAsync();
            if (truck == null) { TempData["ErrorMessage"] = "No trucks available."; return RedirectToPage(); }

            var assignment = new Assignment
            {
                RequestID = req.RequestID,
                TruckID = truck.TruckID,
                AssignedDate = DateTime.Now,
                Truck = truck,
                WasteRequest = req
            };
            _context.Assignments.Add(assignment);
            
            // Update request status
            req.Status = "Assigned";
            
            // Update truck status to Busy
            truck.Status = TruckStatus.Busy;
            
            // First save to generate AssignmentID
            await _context.SaveChangesAsync();

            // Now that AssignmentID is generated, link it as current assignment
            truck.CurrentAssignmentId = assignment.AssignmentID;
            await _context.SaveChangesAsync();
            
            // Send notification to User: "Truck is on the way!"
            if (req.User != null)
            {
                await _notificationService.SendStatusUpdateAsync(
                    req.User.Id,
                    "Truck is on the way!",
                    $"Your waste collection request #{req.RequestID} has been assigned to truck {truck.PlateNumber}. The truck is on the way!",
                    "Success",
                    $"/User/History"
                );
            }
            
            // Send notification to Driver: "New collection assigned!"
            if (truck.Driver != null)
            {
                await _notificationService.SendStatusUpdateAsync(
                    truck.Driver.Id,
                    "New collection assigned!",
                    $"You have been assigned a new collection: Request #{req.RequestID} at {req.Location}. Waste Type: {req.WasteType}",
                    "Info",
                    $"/Driver/Dashboard"
                );
            }
            
            TempData["SuccessMessage"] = $"Assigned request #{req.RequestID} to truck {truck.PlateNumber}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId)
        {
            var req = await _context.WasteRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.RequestID == requestId);
            if (req == null) { TempData["ErrorMessage"] = "Request not found."; return RedirectToPage(); }
            req.Status = "Failed";
            await _context.SaveChangesAsync();
            
            // Send notification
            if (req.User != null)
            {
                await _notificationService.SendStatusUpdateAsync(
                    req.User.Id,
                    "Request Rejected",
                    $"Your waste collection request #{req.RequestID} has been rejected. Please contact support for more information.",
                    "Warning",
                    $"/User/History"
                );
            }
            
            TempData["SuccessMessage"] = $"Rejected request #{req.RequestID}.";
            return RedirectToPage();
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

