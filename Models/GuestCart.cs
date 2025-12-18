using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WasteCollectionSystem.Models
{
    public class GuestCart
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string SessionId { get; set; } = null!;

        [StringLength(20)]
        public string? GuestPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<GuestCartItem> Items { get; set; } = new List<GuestCartItem>();
    }
}
