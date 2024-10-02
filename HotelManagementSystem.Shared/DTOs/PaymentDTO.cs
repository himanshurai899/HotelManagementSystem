using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.DTOs
{
    public class PaymentDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentStatus Status { get; set; }
    }
}
