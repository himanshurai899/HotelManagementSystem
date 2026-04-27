namespace HotelManagementSystem.Shared.DTOs
{
    public class InvoiceItemDTO
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
    }
}
