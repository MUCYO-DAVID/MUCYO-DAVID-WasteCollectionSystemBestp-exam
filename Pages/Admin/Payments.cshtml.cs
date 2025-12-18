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
    public class PaymentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class Row
        {
            public int PaymentID { get; set; }
            public int RequestID { get; set; }
            public string UserName { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime PaymentDate { get; set; }
            public string PaymentStatus { get; set; } = string.Empty;
            public bool IsOverdue { get; set; }
        }

        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public new string? User { get; set; }
        [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;

        public int TotalCount { get; private set; }
        public decimal TotalRevenue { get; private set; }
        public List<Row> Items { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Payments";
            var q = _context.Payments
                .Include(p => p.WasteRequest).ThenInclude(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(p => p.PaymentStatus == Status);
            if (From.HasValue) q = q.Where(p => p.PaymentDate >= From.Value);
            if (To.HasValue) q = q.Where(p => p.PaymentDate <= To.Value);
            if (!string.IsNullOrWhiteSpace(User)) q = q.Where(p => (p.WasteRequest.User != null && p.WasteRequest.User.FullName.Contains(User)) || p.WasteRequest.UserId.Contains(User));

            TotalCount = await q.CountAsync();
            TotalRevenue = await q.Where(p => p.PaymentStatus == "Paid").SumAsync(p => p.Amount);

            var data = await q.OrderByDescending(p => p.PaymentDate)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Items = data.Select(p => new Row
            {
                PaymentID = p.PaymentID,
                RequestID = p.RequestID,
                UserName = p.WasteRequest.User != null ? p.WasteRequest.User.FullName : p.WasteRequest.UserId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentStatus = p.PaymentStatus,
                IsOverdue = p.PaymentStatus != "Paid" && p.PaymentDate < DateTime.Today.AddDays(-7)
            }).ToList();
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var q = _context.Payments.Include(p => p.WasteRequest).ThenInclude(r => r.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(p => p.PaymentStatus == Status);
            if (From.HasValue) q = q.Where(p => p.PaymentDate >= From.Value);
            if (To.HasValue) q = q.Where(p => p.PaymentDate <= To.Value);
            if (!string.IsNullOrWhiteSpace(User)) q = q.Where(p => (p.WasteRequest.User != null && p.WasteRequest.User.FullName.Contains(User)) || p.WasteRequest.UserId.Contains(User));

            var data = await q.OrderByDescending(p => p.PaymentDate).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("PaymentID,User,RequestID,Amount,Date,Status");
            foreach (var p in data)
            {
                sb.AppendLine(string.Join(",", new[] {
                    p.PaymentID.ToString(), EscapeCsv(p.WasteRequest.User?.FullName ?? p.WasteRequest.UserId), p.RequestID.ToString(), p.Amount.ToString("F2"), p.PaymentDate.ToString("yyyy-MM-dd"), p.PaymentStatus
                }));
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "payments.csv");
        }

        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Paid" => "badge bg-success",
                "Pending" => "badge bg-warning text-dark",
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

