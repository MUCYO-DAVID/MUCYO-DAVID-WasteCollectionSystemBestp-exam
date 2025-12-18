using System;
using System.ComponentModel.DataAnnotations;

namespace WasteCollectionSystem.Models
{
    public class Assignment
    {
        [Key]
        public int AssignmentID { get; set; }

        public int RequestID { get; set; }

        public int TruckID { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        public DateTime? CompletionDate { get; set; }

        // Navigation properties — made required + nullable fix for CS8618
        public required WasteRequest WasteRequest { get; set; } = null!;
        public required Truck Truck { get; set; } = null!;
    }
}