using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;
using WasteCollectionSystem.Services;

namespace WasteCollectionSystem.Pages.Driver
{
    [Authorize(Roles = "Driver")]
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

        public ApplicationUser? CurrentDriver { get; private set; }
        public Truck? AssignedTruck { get; private set; }
        public Assignment? CurrentAssignment { get; private set; }
        public List<Assignment> TodaysAssignments { get; private set; } = new();
        public int TodaysCollections { get; private set; }
        public decimal TodaysEarnings { get; private set; }
        public double CompletionRate { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsDriver) return Challenge();

            CurrentDriver = user;

            // Get assigned truck
            AssignedTruck = await _context.Trucks
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.WasteRequest)
                        .ThenInclude(r => r.User)
                .Include(t => t.CurrentAssignment)
                    .ThenInclude(a => a!.WasteRequest)
                        .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(t => t.DriverId == user.Id);

            if (AssignedTruck == null)
            {
                ViewData["Title"] = "Driver Dashboard";
                return Page(); // Show dashboard without truck assignment
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Get today's assignments
            TodaysAssignments = await _context.Assignments
                .Include(a => a.WasteRequest)
                    .ThenInclude(r => r.User)
                .Where(a => a.TruckID == AssignedTruck.TruckID &&
                           a.AssignedDate >= today &&
                           a.AssignedDate < tomorrow)
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();

            // Get current active assignment (not completed)
            CurrentAssignment = AssignedTruck.CurrentAssignment ?? 
                TodaysAssignments.FirstOrDefault(a => a.CompletionDate == null);

            // Statistics
            TodaysCollections = TodaysAssignments.Count(a => a.CompletionDate != null);
            
            // Calculate earnings (dummy calculation: $10 per collection)
            TodaysEarnings = TodaysCollections * 10.00m;

            // Completion rate (collections vs total assignments today)
            var totalToday = TodaysAssignments.Count;
            CompletionRate = totalToday > 0 ? (double)TodaysCollections / totalToday * 100 : 0;

            ViewData["Title"] = "Driver Dashboard";
            return Page();
        }

        public async Task<IActionResult> OnPostStartTripAsync(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsDriver) return Challenge();

            var assignment = await _context.Assignments
                .Include(a => a.WasteRequest)
                .Include(a => a.Truck)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId && a.Truck.DriverId == user.Id);

            if (assignment == null) return NotFound();

            // Update truck status to InTransit
            assignment.Truck.Status = TruckStatus.InTransit;
            assignment.Truck.CurrentAssignmentId = assignmentId;
            
            // Update request status
            assignment.WasteRequest.Status = "In Transit";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Trip started!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkCollectedAsync(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsDriver) return Challenge();

            var assignment = await _context.Assignments
                .Include(a => a.WasteRequest)
                    .ThenInclude(r => r.User)
                .Include(a => a.WasteRequest)
                    .ThenInclude(r => r.Payments)
                .Include(a => a.Truck)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId && a.Truck.DriverId == user.Id);

            if (assignment == null) return NotFound();

            // Mark assignment as completed
            assignment.CompletionDate = DateTime.Now;
            
            // Update truck status back to Available
            assignment.Truck.Status = TruckStatus.Available;
            assignment.Truck.CurrentAssignmentId = null;
            
            // Update request status to Completed
            assignment.WasteRequest.Status = "Completed";

            // Create Payment record if not paid
            var hasPaidPayment = assignment.WasteRequest.Payments.Any(p => p.PaymentStatus == "Paid");
            if (!hasPaidPayment)
            {
                // Calculate amount (dummy: $20 per collection, or use existing payment amount)
                var existingPayment = assignment.WasteRequest.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
                var amount = existingPayment?.Amount ?? 20.00m;
                
                var payment = new Models.Payment
                {
                    RequestID = assignment.WasteRequest.RequestID,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    PaymentStatus = "Pending", // User will pay later
                    WasteRequest = assignment.WasteRequest
                };
                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();
            
            // Notify User: "Waste collected successfully! Thank you!"
            if (assignment.WasteRequest.User != null)
            {
                await _notificationService.SendStatusUpdateAsync(
                    assignment.WasteRequest.User.Id,
                    "Waste collected successfully!",
                    $"Your waste collection request #{assignment.WasteRequest.RequestID} has been collected successfully! Thank you for using our service.",
                    "Success",
                    $"/User/History"
                );
            }
            
            TempData["SuccessMessage"] = "Collection marked as completed!";
            return RedirectToPage();
        }

        public string GetGreeting()
        {
            var hour = DateTime.Now.Hour;
            if (hour < 12) return "Good morning";
            if (hour < 17) return "Good afternoon";
            return "Good evening";
        }
    }
}

