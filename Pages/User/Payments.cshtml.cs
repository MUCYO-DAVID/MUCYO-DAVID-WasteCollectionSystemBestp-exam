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
    public class PaymentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<WasteRequest> UnpaidRequests { get; private set; } = new();
        public List<Models.Payment> PaymentHistory { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Payments";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var requestsQuery = _context.WasteRequests
                .Include(r => r.Payments)
                .Include(r => r.Assignments).ThenInclude(a => a.Truck)
                .Where(r => r.UserId == user.Id);

            var allRequests = await requestsQuery.ToListAsync();
            UnpaidRequests = allRequests
                .Where(r => !r.Payments.Any(p => p.PaymentStatus == "Paid"))
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            PaymentHistory = await _context.Payments
                .Include(p => p.WasteRequest)
                .Where(p => p.WasteRequest.UserId == user.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostPayAsync(int requestId, decimal amount, string cardNo, string expiry, string cvv)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var normalized = new string(cardNo.Where(char.IsDigit).ToArray());
            if (normalized != "4242424242424242" || string.IsNullOrWhiteSpace(expiry) || cvv?.Length != 3)
            {
                TempData["ErrorMessage"] = "Payment failed. Please check card details.";
                return RedirectToPage();
            }

            var request = await _context.WasteRequests.Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.RequestID == requestId && r.UserId == user.Id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToPage();
            }

            var payment = new Models.Payment
            {
                RequestID = request.RequestID,
                Amount = amount,
                PaymentDate = DateTime.Now,
                PaymentStatus = "Paid",
                WasteRequest = request
            };
            _context.Payments.Add(payment);

            request.Status = "Scheduled";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment successful. Your request is scheduled.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetReceiptAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var payment = await _context.Payments.Include(p => p.WasteRequest)
                .FirstOrDefaultAsync(p => p.PaymentID == id && p.WasteRequest.UserId == user.Id);
            if (payment == null) return NotFound();

            var content = $"Payment Receipt\nPaymentID: {payment.PaymentID}\nRequestID: {payment.RequestID}\nAmount: {payment.Amount:F2}\nStatus: {payment.PaymentStatus}\nDate: {payment.PaymentDate:yyyy-MM-dd HH:mm}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(bytes, "application/pdf", $"receipt_{payment.PaymentID}.pdf");
        }
    }
}
