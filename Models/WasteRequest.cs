using System;
using System.ComponentModel.DataAnnotations;

namespace WasteCollectionSystem.Models
{
    public class WasteRequest
    {
        [Key]
        public int RequestID { get; set; }

        // Changed from int UserID → string UserId (Identity uses string)
        public string? UserId { get; set; }

        [StringLength(100)]
        public string? GuestName { get; set; }

        [StringLength(20)]
        public string? GuestPhone { get; set; }

        [Required, StringLength(255)]
        public string Location { get; set; } = null!;

        public string LocationName { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Required, StringLength(100)]
        public string WasteType { get; set; } = null!;

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending";

        public DateTime? PreferredCollectionDateTime { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(260)]
        public string? PhotoPath { get; set; }

        // Navigation — now points to ApplicationUser
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
