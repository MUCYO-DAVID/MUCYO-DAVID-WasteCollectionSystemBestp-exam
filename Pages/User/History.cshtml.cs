using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Pages.User
{
    [Authorize(Roles = "User")]
    public class HistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HistoryModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }

        public List<WasteRequest> Requests { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "History";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var q = _context.WasteRequests
                .Include(r => r.Assignments).ThenInclude(a => a.Truck)
                .Include(r => r.Payments)
                .Where(r => r.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(Status))
            {
                q = q.Where(r => r.Status == Status);
            }

            if (From.HasValue)
            {
                q = q.Where(r => r.RequestDate >= From.Value);
            }

            if (To.HasValue)
            {
                q = q.Where(r => r.RequestDate <= To.Value);
            }

            Requests = await q.OrderByDescending(r => r.RequestDate).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var q = _context.WasteRequests
                .Include(r => r.Assignments).ThenInclude(a => a.Truck)
                .Include(r => r.Payments)
                .Where(r => r.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(r => r.Status == Status);
            if (From.HasValue) q = q.Where(r => r.RequestDate >= From.Value);
            if (To.HasValue) q = q.Where(r => r.RequestDate <= To.Value);

            var data = await q.OrderByDescending(r => r.RequestDate).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("RequestID,Date,Location,WasteType,Status,AssignedTruck,PaymentStatus");
            foreach (var r in data)
            {
                var truck = r.Assignments.OrderByDescending(a => a.AssignedDate).FirstOrDefault()?.Truck;
                var truckLabel = truck != null ? $"{truck.PlateNumber} ({truck.DriverName})" : "";
                var paymentStatus = r.Payments.Any(p => p.PaymentStatus == "Paid") ? "Paid" : r.Payments.Any() ? "Pending" : "None";
                var line = string.Join(",", new[]
                {
                    r.RequestID.ToString(),
                    r.RequestDate.ToString("yyyy-MM-dd HH:mm"),
                    EscapeCsv(r.Location),
                    EscapeCsv(r.WasteType),
                    EscapeCsv(r.Status),
                    EscapeCsv(truckLabel),
                    EscapeCsv(paymentStatus)
                });
                sb.AppendLine(line);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "waste_requests.csv");
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
