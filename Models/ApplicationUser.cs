using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WasteCollectionSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = null!;

        // PhoneNumber property - synced with IdentityUser.PhoneNumber
        // Also keeping Phone for backward compatibility
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        public string Role { get; set; } = "User"; // User, Admin, Driver, Manager, Supervisor

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<WasteRequest> WasteRequests { get; set; } =
            new List<WasteRequest>();
        
        // Navigation to assigned truck (for drivers)
        public virtual Truck? AssignedTruck { get; set; }
        
        // Helper to check if user is a driver
        public bool IsDriver => Role == "Driver";
        
        // Helper to get display name with fallback
        public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email ?? UserName ?? "User";
        
        // Helper to get phone number (prioritizes PhoneNumber, falls back to Phone)
        public string? GetPhoneNumber() => !string.IsNullOrEmpty(PhoneNumber) ? PhoneNumber : Phone;
        
        // Helper to set phone number (updates both properties)
        public void SetPhoneNumber(string? phoneNumber)
        {
            PhoneNumber = phoneNumber;
            Phone = phoneNumber;
        }
    }
}
