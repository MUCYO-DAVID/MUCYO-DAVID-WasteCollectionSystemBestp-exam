using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Pages.User
{
    [Authorize]
    public class NotificationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Notification> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }

        public class NotificationGroup
        {
            public string Title { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public List<Notification> Items { get; set; } = new();
        }

        public List<NotificationGroup> NotificationGroups { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            Notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            UnreadCount = Notifications.Count(n => !n.IsRead);

            // Group notifications
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var weekAgo = today.AddDays(-7);

            var todayItems = Notifications.Where(n => n.CreatedAt.Date == today).ToList();
            var yesterdayItems = Notifications.Where(n => n.CreatedAt.Date == yesterday).ToList();
            var olderItems = Notifications.Where(n => n.CreatedAt.Date < yesterday).ToList();

            if (todayItems.Any())
            {
                NotificationGroups.Add(new NotificationGroup
                {
                    Title = "Today",
                    Icon = "fa-calendar-day",
                    Items = todayItems
                });
            }

            if (yesterdayItems.Any())
            {
                NotificationGroups.Add(new NotificationGroup
                {
                    Title = "Yesterday",
                    Icon = "fa-calendar",
                    Items = yesterdayItems
                });
            }

            if (olderItems.Any())
            {
                NotificationGroups.Add(new NotificationGroup
                {
                    Title = "Older",
                    Icon = "fa-calendar-check",
                    Items = olderItems
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Marked {notifications.Count} notification(s) as read.";
            return RedirectToPage();
        }

        public string GetNotificationIcon(string type)
        {
            return type switch
            {
                "Success" => "fa-check-circle text-success",
                "Warning" => "fa-exclamation-triangle text-warning",
                "Error" => "fa-times-circle text-danger",
                _ => "fa-info-circle text-info"
            };
        }

        public string GetTimeAgo(DateTime date)
        {
            var now = DateTime.UtcNow;
            var diff = now - date;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min{(diff.TotalMinutes >= 2 ? "s" : "")} ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hour{(diff.TotalHours >= 2 ? "s" : "")} ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} day{(diff.TotalDays >= 2 ? "s" : "")} ago";
            return date.ToString("MMM dd, yyyy");
        }
    }
}

