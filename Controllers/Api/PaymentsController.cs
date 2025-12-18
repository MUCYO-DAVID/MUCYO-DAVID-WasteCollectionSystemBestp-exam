using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WasteCollectionSystem.Models;
using WasteCollectionSystem.Services;

namespace WasteCollectionSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PaymentsController : ControllerBase
    {
        private readonly IMomoPaymentService _momoService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(IMomoPaymentService momoService, UserManager<ApplicationUser> userManager)
        {
            _momoService = momoService;
            _userManager = userManager;
        }

        [HttpPost("pay")]
        public async Task<IActionResult> PayWithMoMo([FromBody] PaymentRequestDto model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try 
            {
                // Initiate payment - returns reference ID string on success, throws on error
                var transactionId = await _momoService.RequestToPayAsync(model.PhoneNumber, model.Amount);

                return Ok(new { message = "Payment initiated successfully", transactionId = transactionId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Payment failed", error = ex.Message });
            }
        }
    }

    public class PaymentRequestDto
    {
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
