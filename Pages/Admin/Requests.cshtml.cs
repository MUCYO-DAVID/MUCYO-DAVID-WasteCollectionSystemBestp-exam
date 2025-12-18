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
    public class RequestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public RequestsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public class Row
        {
            public int RequestID { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string? UserPhone { get; set; }
            public string Location { get; set; } = string.Empty;
            public string WasteType { get; set; } = string.Empty;
            public DateTime RequestDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public string PaymentStatus { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public string? Payment { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public new string? User { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;

        public int TotalCount { get; private set; }
        public List<Row> Items { get; private set; } = new();
        public List<Truck> Trucks { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Requests";
            var q = _context.WasteRequests
                .Include(r => r.User)
                .Include(r => r.Payments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(r => r.Status == Status);
            if (!string.IsNullOrWhiteSpace(Payment))
            {
                if (Payment == "Paid") q = q.Where(r => r.Payments.Any(p => p.PaymentStatus == "Paid"));
                else if (Payment == "Pending") q = q.Where(r => r.Payments.Any() && !r.Payments.Any(p => p.PaymentStatus == "Paid"));
                else if (Payment == "None") q = q.Where(r => !r.Payments.Any());
            }
            if (From.HasValue) q = q.Where(r => r.RequestDate >= From.Value);
            if (To.HasValue) q = q.Where(r => r.RequestDate <= To.Value);
            if (!string.IsNullOrWhiteSpace(User)) q = q.Where(r => (r.User != null && r.User.FullName.Contains(User)) || r.UserId.Contains(User));
            if (!string.IsNullOrWhiteSpace(Search)) q = q.Where(r => r.Location.Contains(Search) || r.WasteType.Contains(Search) || (r.User != null && (r.User.PhoneNumber != null && r.User.PhoneNumber.Contains(Search) || (r.User.Phone != null && r.User.Phone.Contains(Search)))));

            TotalCount = await q.CountAsync();
            var data = await q.OrderByDescending(r => r.RequestDate)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Items = data.Select(r => new Row
            {
                RequestID = r.RequestID,
                UserName = r.User != null ? r.User.FullName : (r.GuestName ?? "Guest"),
                UserPhone = r.User != null ? r.User.GetPhoneNumber() : r.GuestPhone,
                Location = r.Location,
                WasteType = r.WasteType,
                RequestDate = r.RequestDate,
                Status = r.Status,
                PaymentStatus = r.Payments.Any(p => p.PaymentStatus == "Paid") ? "Paid" : r.Payments.Any() ? "Pending" : "None",
                Latitude = r.Latitude,
                Longitude = r.Longitude
            }).ToList();

            Trucks = await _context.Trucks.OrderBy(t => t.PlateNumber).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var req = await _context.WasteRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.RequestID == id);
            if (req == null) return NotFound();
            req.Status = "In Progress";
            await _context.SaveChangesAsync();
            
            // Send notification
            if (req.User != null)
            {
                await _notificationService.SendStatusUpdateAsync(
                    req.User.Id,
                    "Request Approved",
                    $"Your waste collection request #{id} has been approved and is now in progress.",
                    "Success",
                    $"/User/History"
                );
            }
            
            TempData["SuccessMessage"] = $"Approved request #{id}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var req = await _context.WasteRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.RequestID == id);
            if (req == null) return NotFound();
            req.Status = "Failed";
            await _context.SaveChangesAsync();
            
            // Send notification
            if (req.User != null)
            {
                await _notificationService.SendStatusUpdateAsync(
                    req.User.Id,
                    "Request Rejected",
                    $"Your waste collection request #{id} has been rejected. Please contact support for more information.",
                    "Warning",
                    $"/User/History"
                );
            }
            
            TempData["SuccessMessage"] = $"Rejected request #{id}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignAsync(int id, int truckId)
        {
            var req = await _context.WasteRequests
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
                    $"Your waste collection request #{id} has been assigned to truck {truck.PlateNumber}. The truck is on the way!",
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
                    $"You have been assigned a new collection: Request #{id} at {req.Location}. Waste Type: {req.WasteType}",
                    "Info",
                    $"/Driver/Dashboard"
                );
            }
            
            TempData["SuccessMessage"] = $"Assigned request #{id} to truck {truck.PlateNumber}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var req = await _context.WasteRequests.Include(r => r.Payments).Include(r => r.Assignments).FirstOrDefaultAsync(r => r.RequestID == id);
            if (req == null) return NotFound();
            _context.Payments.RemoveRange(req.Payments);
            _context.Assignments.RemoveRange(req.Assignments);
            _context.WasteRequests.Remove(req);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Deleted request #{id}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBulkApproveAsync(int[] selected)
        {
            var reqs = await _context.WasteRequests.Where(r => selected.Contains(r.RequestID)).ToListAsync();
            foreach (var r in reqs) r.Status = "In Progress";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Approved {reqs.Count} requests.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBulkDeleteAsync(int[] selected)
        {
            var reqs = await _context.WasteRequests.Include(r => r.Payments).Include(r => r.Assignments).Where(r => selected.Contains(r.RequestID)).ToListAsync();
            foreach (var r in reqs)
            {
                _context.Payments.RemoveRange(r.Payments);
                _context.Assignments.RemoveRange(r.Assignments);
            }
            _context.WasteRequests.RemoveRange(reqs);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Deleted {reqs.Count} requests.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var q = _context.WasteRequests.Include(r => r.User).Include(r => r.Payments).AsQueryable();
            if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(r => r.Status == Status);
            if (!string.IsNullOrWhiteSpace(Payment))
            {
                if (Payment == "Paid") q = q.Where(r => r.Payments.Any(p => p.PaymentStatus == "Paid"));
                else if (Payment == "Pending") q = q.Where(r => r.Payments.Any() && !r.Payments.Any(p => p.PaymentStatus == "Paid"));
                else if (Payment == "None") q = q.Where(r => !r.Payments.Any());
            }
            if (From.HasValue) q = q.Where(r => r.RequestDate >= From.Value);
            if (To.HasValue) q = q.Where(r => r.RequestDate <= To.Value);
            if (!string.IsNullOrWhiteSpace(User)) q = q.Where(r => (r.User != null && r.User.FullName.Contains(User)) || r.UserId.Contains(User));
            if (!string.IsNullOrWhiteSpace(Search)) q = q.Where(r => r.Location.Contains(Search) || r.WasteType.Contains(Search) || (r.User != null && (r.User.PhoneNumber != null && r.User.PhoneNumber.Contains(Search) || (r.User.Phone != null && r.User.Phone.Contains(Search)))));

            var data = await q.OrderByDescending(r => r.RequestDate).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID,User,Phone,Location,WasteType,Date,Status,PaymentStatus");
            foreach (var r in data)
            {
                var pay = r.Payments.Any(p => p.PaymentStatus == "Paid") ? "Paid" : r.Payments.Any() ? "Pending" : "None";
                sb.AppendLine(string.Join(",", new[] {
                    r.RequestID.ToString(), EscapeCsv(r.User?.FullName ?? r.UserId), EscapeCsv(r.User?.GetPhoneNumber() ?? ""), EscapeCsv(r.Location), EscapeCsv(r.WasteType), r.RequestDate.ToString("yyyy-MM-dd"), EscapeCsv(r.Status), pay
                }));
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "requests.csv");
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

        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n');
            var t = s.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{t}\"" : t;
        }
    }
}

