using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.Models
{
    // Invoice.cs
    // NOTE: In Phase 9 (Multi-Tenant SaaS) this entity will gain a TenantId FK.
    public class Invoice
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }

        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}
