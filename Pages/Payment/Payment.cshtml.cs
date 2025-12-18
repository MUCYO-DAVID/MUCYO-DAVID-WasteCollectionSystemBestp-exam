using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WasteCollectionSystem.Services;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace WasteCollectionSystem.Pages.Payment
{
    public class PaymentModel : PageModel
    {
        private readonly MtnMomoService _momoService;
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;
        private readonly CartService _cartService; // If needed for helper methods, but we can do DB direct

        public PaymentModel(MtnMomoService momoService, ApplicationDbContext context, Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender)
        {
            _momoService = momoService;
            _context = context;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required(ErrorMessage = "Amount is required")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
            [Display(Name = "Amount (EUR)")]
            public decimal Amount { get; set; }

            [Required(ErrorMessage = "Email is required for receipt")]
            [EmailAddress]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Invalid phone number format")]
            [Display(Name = "Phone Number (MSISDN)")]
            [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Phone number must be 8-15 digits")]
            public string PayerMsisdn { get; set; } = string.Empty;
        }

        [BindProperty]
        public string? ItemIds { get; set; }

        public void OnGet(decimal? amount, string items, int? requestId)
        {
            // Pre-populate amount if coming from cart
            if (amount.HasValue && amount.Value > 0)
            {
                Input = new InputModel
                {
                    Amount = amount.Value,
                    PayerMsisdn = string.Empty,
                    // Try to pre-fill email if user is logged in
                    Email = User.Identity?.Name ?? string.Empty
                };
            }
            if (!string.IsNullOrEmpty(items))
            {
                ItemIds = items;
            }
            else if (requestId.HasValue)
            {
                ItemIds = requestId.Value.ToString();
            }
        }

        /// <summary>
        /// Handler to initiate payment via AJAX.
        /// Returns JSON with transactionId for status polling.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "Invalid input. Please check your form."
                });
            }

            try
            {
                // Call MtnMomoService to initiate payment
                var transactionId = await _momoService.RequestToPayAsync(
                    Input.PayerMsisdn,
                    Input.Amount
                );

                return new JsonResult(new
                {
                    success = true,
                    transactionId = transactionId,
                    itemIds = ItemIds
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = $"Payment initiation failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Handler to check payment status via AJAX polling.
        /// Returns JSON with current transaction status.
        /// </summary>
        public async Task<IActionResult> OnGetCheckStatusAsync(string transactionId, string? itemIds, string? email)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "Transaction ID is required"
                });
            }

            try
            {
                // Call MtnMomoService to get transaction status
                var result = await _momoService.GetTransactionStatusAsync(transactionId);
                
                // If successful, update the database
                if (result.Status == "SUCCESSFUL" && !string.IsNullOrEmpty(itemIds))
                {
                    await ProcessSuccessfulPayment(itemIds, transactionId, result.Amount, email);
                }

                return new JsonResult(new
                {
                    success = true,
                    status = result.Status
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = $"Status check failed: {ex.Message}"
                });
            }
        }

        private async Task ProcessSuccessfulPayment(string itemIds, string transactionId, decimal totalAmount, string? email)
        {
             var ids = itemIds.Split(',').Select(id => int.TryParse(id, out var i) ? i : 0).Where(i => i > 0).ToList();
             if (!ids.Any()) return;

             var requests = await _context.WasteRequests.Include(r => r.Payments).Where(r => ids.Contains(r.RequestID)).ToListAsync();
             
             // Distribute amount evenly (or based on some logic, but even is fine for now)
             decimal amountPerRequest = ids.Count > 0 ? totalAmount / ids.Count : 0;

             foreach (var req in requests)
             {
                 // Update request status if not already paid
                 if (req.Status != "Paid")
                 {
                     req.Status = "Paid"; 
                 }

                 // Check if payment exists
                 var existingPayment = req.Payments.FirstOrDefault(p => p.PaymentStatus == "Paid" && p.PaymentDate > DateTime.Now.AddMinutes(-5));
                 
                 if (existingPayment == null)
                 {
                     _context.Payments.Add(new WasteCollectionSystem.Models.Payment
                     {
                         RequestID = req.RequestID,
                         Amount = amountPerRequest,
                         PaymentStatus = "Paid",
                         PaymentDate = DateTime.Now,
                         WasteRequest = req
                     });
                 }
             }
             await _context.SaveChangesAsync();

             // Remove from guest cart if exists
             // We can check if these requests are in any guest cart and remove them
             var guestCartItems = await _context.GuestCartItems
                 .Where(gci => ids.Contains(gci.WasteRequestId))
                 .ToListAsync();
             
             if (guestCartItems.Any())
             {
                 _context.GuestCartItems.RemoveRange(guestCartItems);
                 await _context.SaveChangesAsync();
             }

             // Send Email Receipt to User
             if (!string.IsNullOrEmpty(email))
             {
                 try
                 {
                     Console.WriteLine($"Attempting to send email to user: {email}");
                     await _emailSender.SendEmailAsync(email, "Payment Confirmation - WasteCollect", 
                         $"<h3>Payment Successful</h3><p>Your payment of EUR {totalAmount:F2} for transaction {transactionId} was successful.</p><p>Thank you for using WasteCollect.</p>");
                     Console.WriteLine("User email sent successfully.");
                 }
                 catch (Exception ex) 
                 { 
                     Console.WriteLine($"Failed to send user email: {ex.Message}");
                 }
             }

             // Send Email Receipt to Admin
             try
             {
                 Console.WriteLine("Attempting to send email to admin: emile.tester10@gmail.com");
                 await _emailSender.SendEmailAsync("emile.tester10@gmail.com", "New Payment Received - WasteCollect", 
                     $"<h3>New Payment Received</h3><p>A payment of EUR {totalAmount:F2} has been received.</p><p><strong>Transaction ID:</strong> {transactionId}</p><p><strong>Payer Email:</strong> {email ?? "Not provided"}</p>");
                 Console.WriteLine("Admin email sent successfully.");
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Failed to send admin email: {ex.Message}");
             }
        }
    }
}