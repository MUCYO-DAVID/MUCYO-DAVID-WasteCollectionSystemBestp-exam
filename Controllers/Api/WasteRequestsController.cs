using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class WasteRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WasteRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (user.Role == "Admin")
            {
                var allRequests = await _context.WasteRequests
                    .Include(r => r.User)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
                return Ok(allRequests);
            }

            var myRequests = await _context.WasteRequests
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return Ok(myRequests);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateWasteRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var wasteRequest = new WasteRequest
            {
                UserId = user.Id,
                WasteType = model.WasteType,
                // Amount is not in DB model, appending to Notes
                RequestDate = DateTime.Now,
                Status = "Pending",
                Location = model.Location,
                Notes = $"{model.Notes} (Amount: {model.Amount})",
                User = user // Set navigation property
            };
            
            // Price calculation removed as it's not in the model

            _context.WasteRequests.Add(wasteRequest);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Request created successfully", requestId = wasteRequest.RequestID });
        }
    }

    public class CreateWasteRequestDto
    {
        public string WasteType { get; set; }
        public string Amount { get; set; }
        public string Location { get; set; }
        public string Notes { get; set; }
    }
}
