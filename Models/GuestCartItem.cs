using System;
using System.ComponentModel.DataAnnotations;

namespace WasteCollectionSystem.Models
{
    public class GuestCartItem
    {
        [Key]
        public int Id { get; set; }

        public int GuestCartId { get; set; }
        public virtual GuestCart GuestCart { get; set; } = null!;

        public int WasteRequestId { get; set; }
        public virtual WasteRequest WasteRequest { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}
