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
    public class TrucksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrucksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("assign")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> AssignTruck([FromBody] AssignTruckDto model)
        {
            var request = await _context.WasteRequests.FindAsync(model.RequestId);
            if (request == null) return NotFound("Request not found");

            var truck = await _context.Trucks.FindAsync(model.TruckId);
            if (truck == null) return NotFound("Truck not found");

            // Create assignment
            var assignment = new Assignment
            {
                RequestID = request.RequestID,
                TruckID = truck.TruckID,
                AssignedDate = DateTime.Now,
                // Status not in Assignment model
                WasteRequest = request, // Set required navigation property
                Truck = truck          // Set required navigation property
            };

            _context.Assignments.Add(assignment);
            
            // Update request status
            request.Status = "Assigned";
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Truck assigned successfully" });
        }

        [HttpGet("my-truck")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Driver")]
        public async Task<IActionResult> GetMyTruck()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var truck = await _context.Trucks
                .FirstOrDefaultAsync(t => t.DriverId == user.Id);

            if (truck == null) return NotFound("No truck assigned to this driver");

            return Ok(truck);
        }
    }

    public class AssignTruckDto
    {
        public int RequestId { get; set; }
        public int TruckId { get; set; }
    }
}
