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
    public class TrucksModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrucksModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class TruckRow
        {
            public int TruckID { get; set; }
            public string PlateNumber { get; set; } = string.Empty;
            public string? DriverId { get; set; }
            public string DriverName { get; set; } = string.Empty;
            public TruckStatus Status { get; set; }
            public int TotalAssignments { get; set; }
            public Assignment? CurrentAssignment { get; set; }
        }

        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }

        public List<TruckRow> Trucks { get; private set; } = new();
        public List<ApplicationUser> Drivers { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Trucks";
            var q = _context.Trucks
                .Include(t => t.Assignments)
                .Include(t => t.Driver)
                .Include(t => t.CurrentAssignment)
                .AsQueryable();
            
            // Filter by status if provided (parse string to enum)
            if (!string.IsNullOrWhiteSpace(Status) && Enum.TryParse<TruckStatus>(Status, out var statusEnum))
            {
                q = q.Where(t => t.Status == statusEnum);
            }
            
            if (!string.IsNullOrWhiteSpace(Search))
            {
                q = q.Where(t => t.PlateNumber.Contains(Search) || 
                    (t.DriverName != null && t.DriverName.Contains(Search)) ||
                    (t.Driver != null && (t.Driver.FullName.Contains(Search) || t.Driver.Email!.Contains(Search))));
            }

            var data = await q.OrderBy(t => t.PlateNumber).ToListAsync();
            Trucks = data.Select(t => new TruckRow
            {
                TruckID = t.TruckID,
                PlateNumber = t.PlateNumber,
                DriverId = t.DriverId,
                DriverName = t.DisplayDriverName,
                Status = t.Status,
                TotalAssignments = t.Assignments.Count,
                CurrentAssignment = t.CurrentAssignment ?? t.Assignments.OrderByDescending(a => a.AssignedDate).FirstOrDefault(a => a.CompletionDate == null)
            }).ToList();

            // Filter drivers by Role instead of IsDriver (EF can't translate computed properties)
            Drivers = await _userManager.Users
                .Where(u => u.Role == "Driver")
                .OrderBy(u => u.FullName)
                .ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(string plateNumber, string? driverUserId, string? driverName)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) { TempData["ErrorMessage"] = "Plate Number is required."; return RedirectToPage(); }
            
            var truck = new Truck 
            { 
                PlateNumber = plateNumber, 
                Status = TruckStatus.Available 
            };
            
            // Set driver if provided
            if (!string.IsNullOrEmpty(driverUserId))
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == driverUserId);
                // Check driver role using Role field (IsDriver is not EF-translatable)
                if (user != null && user.Role == "Driver")
                {
                    truck.DriverId = driverUserId;
                    truck.DriverName = user.DisplayName; // Keep for backward compatibility
                }
            }
            else if (!string.IsNullOrEmpty(driverName))
            {
                truck.DriverName = driverName;
            }
            
            _context.Trucks.Add(truck);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Truck added.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, string plateNumber, string? driverUserId, string? driverName)
        {
            var t = await _context.Trucks.FindAsync(id);
            if (t == null) return NotFound();
            t.PlateNumber = plateNumber;
            
            // Update driver assignment
            if (!string.IsNullOrEmpty(driverUserId))
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == driverUserId);
                // Check driver role using Role field (IsDriver is not EF-translatable)
                if (user != null && user.Role == "Driver")
                {
                    t.DriverId = driverUserId;
                    t.DriverName = user.DisplayName; // Keep for backward compatibility
                }
            }
            else if (!string.IsNullOrEmpty(driverName))
            {
                t.DriverId = null; // Clear driver assignment
                t.DriverName = driverName;
            }
            else
            {
                t.DriverId = null;
                t.DriverName = null;
            }
            
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Truck updated.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var t = await _context.Trucks.Include(x => x.Assignments).FirstOrDefaultAsync(x => x.TruckID == id);
            if (t == null) return NotFound();
            _context.Assignments.RemoveRange(t.Assignments);
            _context.Trucks.Remove(t);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Truck deleted.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostStatusAsync(int id, string status)
        {
            var t = await _context.Trucks.FindAsync(id);
            if (t == null) return NotFound();
            
            // Parse string to enum
            if (Enum.TryParse<TruckStatus>(status, out var statusEnum))
            {
                t.Status = statusEnum;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Truck status updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid status value.";
            }
            
            return RedirectToPage();
        }

        public string GetStatusBadgeClass(TruckStatus status)
        {
            return status switch
            {
                TruckStatus.Available => "badge bg-success",
                TruckStatus.Busy => "badge bg-warning text-dark",
                TruckStatus.InTransit => "badge bg-info",
                TruckStatus.Maintenance => "badge bg-secondary",
                _ => "badge bg-light text-dark"
            };
        }
    }
}

