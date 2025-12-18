using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public NotificationService(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Sends both email and creates a notification for status updates
        /// </summary>
        public async Task SendStatusUpdateAsync(string userId, string title, string message, string type = "Info", string? url = null)
        {
            // Create notification
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Url = url,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email if user exists and has email
            var user = await _context.Users.FindAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    var emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #2c8558;'>{title}</h2>
                            <p>{message}</p>
                            {(url != null ? $"<p><a href='{url}' style='background-color: #2c8558; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>View Details</a></p>" : "")}
                        </div>";
                    
                    await _emailSender.SendEmailAsync(user.Email, title, emailBody);
                }
                catch
                {
                    // Email sending failed, but notification is saved
                    // Don't throw - notification is more important
                }
            }
        }

        /// <summary>
        /// Creates a notification without sending email
        /// </summary>
        public async Task CreateNotificationAsync(string userId, string title, string message, string type = "Info", string? url = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Url = url,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Marks all notifications as read for a user
        /// </summary>
        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets unread count for a user
        /// </summary>
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}

