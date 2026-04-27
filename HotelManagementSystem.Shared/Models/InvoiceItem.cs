namespace HotelManagementSystem.Shared.Models
{
    // InvoiceItem.cs
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
    }
}
