using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.DTOs
{
    public class BookingDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // To avoid complex object graph
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty; // To avoid complex object graph
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; }
        public int TenantId { get; set; }
        public string? TenantName { get; set; } // To avoid complex object graph
    }
}
