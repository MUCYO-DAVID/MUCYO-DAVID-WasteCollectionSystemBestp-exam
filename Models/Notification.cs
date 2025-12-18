using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WasteCollectionSystem.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Error

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Url { get; set; }

        // Navigation
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }
    }
}

