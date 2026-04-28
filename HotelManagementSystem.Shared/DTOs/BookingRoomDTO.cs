namespace HotelManagementSystem.Shared.DTOs
{
    // BookingRoomDTO.cs
    public class BookingRoomDTO
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty; // To avoid complex object graph
        public decimal PriceAtBooking { get; set; }
    }
}
