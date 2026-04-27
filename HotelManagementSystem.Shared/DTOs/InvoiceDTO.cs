using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.DTOs
{
    public class InvoiceDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string GuestName { get; set; } = string.Empty;   // To avoid complex object graph
        public string RoomNumber { get; set; } = string.Empty;  // To avoid complex object graph
        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
        public List<InvoiceItemDTO> Items { get; set; } = new();
    }
}
