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
    public class RequestDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public RequestDetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }


        public WasteRequest CurrentRequest { get; private set; } = null!;
        public ApplicationUser? CurrentUser { get; private set; }
        public List<Truck> Trucks { get; private set; } = new();
        public List<Models.Payment> Payments { get; private set; } = new();
        public Assignment? LatestAssignment { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var req = await _context.WasteRequests
                .Include(r => r.User)
                .Include(r => r.Assignments).ThenInclude(a => a.Truck)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.RequestID == id);
            if (req == null) return NotFound();

            CurrentRequest = req;
            CurrentUser = req.User;
            Payments = req.Payments.OrderByDescending(p => p.PaymentDate).ToList();
            LatestAssignment = req.Assignments.OrderByDescending(a => a.AssignedDate).FirstOrDefault();
            Trucks = await _context.Trucks.OrderBy(t => t.PlateNumber).ToListAsync();
            ViewData["Title"] = "Request Details";
            return Page();
        }

        public async Task<IActionResult> OnPostAssignAsync(int id, int truckId)
        {
            var req = await _context.WasteRequests
                .Include(r => r.Assignments)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestID == id);
            var truck = await _context.Trucks
                .Include(t => t.Driver)
                .FirstOrDefaultAsync(t => t.TruckID == truckId);
            if (req == null || truck == null) return NotFound();

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
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostStatusAsync(int id, string status)
        {
            var req = await _context.WasteRequests
                .Include(r => r.Assignments)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestID == id);
            if (req == null) return NotFound();

            var oldStatus = req.Status;
            req.Status = status;
            var latest = req.Assignments.OrderByDescending(a => a.AssignedDate).FirstOrDefault();
            if ((status == "Collected" || status == "Completed") && latest != null)
            {
                latest.CompletionDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            
            // Send notification if status changed and user exists
            if (req.User != null && oldStatus != status)
            {
                var (title, message, type) = GetStatusNotificationDetails(status, req.RequestID);
                await _notificationService.SendStatusUpdateAsync(
                    req.User.Id,
                    title,
                    message,
                    type,
                    $"/User/History"
                );
            }
            
            TempData["SuccessMessage"] = $"Status updated to {status}.";
            return RedirectToPage(new { id });
        }

        private (string title, string message, string type) GetStatusNotificationDetails(string status, int requestId)
        {
            return status switch
            {
                "Approved" => ("Request Approved", $"Your waste collection request #{requestId} has been approved.", "Success"),
                "Assigned" => ("Request Assigned", $"Your waste collection request #{requestId} has been assigned to a truck.", "Success"),
                "In Transit" => ("Collection In Progress", $"Your waste collection request #{requestId} is now in transit.", "Info"),
                "Collected" => ("Waste Collected", $"Your waste collection request #{requestId} has been collected.", "Success"),
                "Completed" => ("Request Completed", $"Your waste collection request #{requestId} has been completed.", "Success"),
                "Failed" => ("Request Failed", $"Your waste collection request #{requestId} could not be completed.", "Warning"),
                _ => ("Status Updated", $"Your waste collection request #{requestId} status has been updated to {status}.", "Info")
            };
        }

        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending" => "badge bg-warning text-dark",
                "Approved" => "badge bg-primary",
                "Assigned" => "badge bg-orange",
                "In Transit" => "badge bg-info",
                "Collected" => "badge bg-success",
                "Completed" => "badge bg-success",
                "Failed" => "badge bg-danger",
                _ => "badge bg-secondary"
            };
        }
    }
}
