using System;
using System.ComponentModel.DataAnnotations;

namespace WasteCollectionSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        public int RequestID { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed

        // Navigation
        public required WasteRequest WasteRequest { get; set; }
    }
}