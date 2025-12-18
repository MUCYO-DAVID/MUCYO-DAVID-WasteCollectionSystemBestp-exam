using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public class Row
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string Role { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        [BindProperty(SupportsGet = true)] public string? RoleFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;

        public int TotalCount { get; private set; }
        public List<Row> Items { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Users";
            var q = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(RoleFilter)) q = q.Where(u => u.Role == RoleFilter);
            if (!string.IsNullOrWhiteSpace(StatusFilter)) q = q.Where(u => u.Status == StatusFilter);
            if (!string.IsNullOrWhiteSpace(Search)) q = q.Where(u => u.FullName.Contains(Search) || u.Email!.Contains(Search) || (u.PhoneNumber != null && u.PhoneNumber.Contains(Search)) || (u.Phone != null && u.Phone.Contains(Search)));

            TotalCount = await q.CountAsync();
            var data = await q.OrderByDescending(u => u.CreatedAt)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Items = data.Select(u => new Row
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? u.UserName ?? "",
                Phone = u.GetPhoneNumber(),
                Role = u.Role,
                Status = u.Status,
                CreatedAt = u.CreatedAt
            }).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateRoleAsync(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.Role = role;
            // Sync Identity roles
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any()) await _userManager.RemoveFromRolesAsync(user, roles);
            if (!await _roleManager.RoleExistsAsync(role)) await _roleManager.CreateAsync(new IdentityRole(role));
            await _userManager.AddToRoleAsync(user, role);
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Role updated.";
            return RedirectToPage(new { RoleFilter, StatusFilter, Search, Page, PageSize });
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(string id, string status)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.Status = status;
            if (status == "Blocked")
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            }
            else
            {
                user.LockoutEnd = null;
            }
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Status updated.";
            return RedirectToPage(new { RoleFilter, StatusFilter, Search, Page, PageSize });
        }

        public async Task<IActionResult> OnPostImpersonateAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);
            if (user.Role == "Admin") return RedirectToPage("/Admin/Dashboard");
            return RedirectToPage("/User/UserDashboard");
        }

        public async Task<IActionResult> OnPostBulkBlockAsync(string[] selected)
        {
            var users = await _userManager.Users.Where(u => selected.Contains(u.Id)).ToListAsync();
            foreach (var u in users)
            {
                u.Status = "Blocked";
                u.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            }
            foreach (var u in users) await _userManager.UpdateAsync(u);
            TempData["SuccessMessage"] = $"Blocked {users.Count} users.";
            return RedirectToPage(new { RoleFilter, StatusFilter, Search, Page, PageSize });
        }

        public async Task<IActionResult> OnPostBulkDeleteAsync(string[] selected)
        {
            var users = await _userManager.Users.Where(u => selected.Contains(u.Id)).ToListAsync();
            foreach (var u in users) await _userManager.DeleteAsync(u);
            TempData["SuccessMessage"] = $"Deleted {users.Count} users.";
            return RedirectToPage(new { RoleFilter, StatusFilter, Search, Page, PageSize });
        }

        public async Task<IActionResult> OnPostBulkChangeRoleAsync(string[] selected, string role)
        {
            var users = await _userManager.Users.Where(u => selected.Contains(u.Id)).ToListAsync();
            foreach (var u in users)
            {
                u.Role = role;
                var roles = await _userManager.GetRolesAsync(u);
                if (roles.Any()) await _userManager.RemoveFromRolesAsync(u, roles);
                if (!await _roleManager.RoleExistsAsync(role)) await _roleManager.CreateAsync(new IdentityRole(role));
                await _userManager.AddToRoleAsync(u, role);
                await _userManager.UpdateAsync(u);
            }
            TempData["SuccessMessage"] = $"Changed role to {role} for {users.Count} users.";
            return RedirectToPage(new { RoleFilter, StatusFilter, Search, Page, PageSize });
        }

        public async Task<IActionResult> OnPostAddAsync(string fullName, string email, string? phone, string role)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "FullName and Email are required.";
                return RedirectToPage();
            }
            var user = new ApplicationUser
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                Role = role,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            // Set phone number using helper method to sync both properties
            if (!string.IsNullOrWhiteSpace(phone))
            {
                user.SetPhoneNumber(phone);
            }
            var result = await _userManager.CreateAsync(user, "P@ssword123!");
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToPage();
            }
            // Ensure PhoneNumber is also set in Identity
            if (!string.IsNullOrWhiteSpace(phone))
            {
                await _userManager.SetPhoneNumberAsync(user, phone);
            }
            if (!await _roleManager.RoleExistsAsync(role)) await _roleManager.CreateAsync(new IdentityRole(role));
            await _userManager.AddToRoleAsync(user, role);
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "User added.";
            return RedirectToPage();
        }
    }
}

