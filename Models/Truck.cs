using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WasteCollectionSystem.Models
{
    public class Truck
    {
        [Key]
        public int TruckID { get; set; }

        [Required, StringLength(20)]
        public string PlateNumber { get; set; } = null!;

        [StringLength(150)]
        public string? DriverName { get; set; } // Optional, can be derived from Driver

        public TruckStatus Status { get; set; } = TruckStatus.Available;

        // Foreign key to ApplicationUser (Driver)
        [StringLength(450)] // Identity user IDs are typically 450 chars
        public string? DriverId { get; set; }

        // Navigation property to Driver (ApplicationUser)
        [ForeignKey(nameof(DriverId))]
        public virtual ApplicationUser? Driver { get; set; }

        // Current active assignment (nullable)
        public int? CurrentAssignmentId { get; set; }

        // Navigation property to current assignment
        [ForeignKey(nameof(CurrentAssignmentId))]
        public virtual Assignment? CurrentAssignment { get; set; }

        // All assignments for this truck
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

        // Helper property to get driver name (from Driver or DriverName)
        [NotMapped]
        public string DisplayDriverName => Driver?.DisplayName ?? DriverName ?? "Unassigned";
    }
}
