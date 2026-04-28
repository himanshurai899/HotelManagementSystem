namespace HotelManagementSystem.Shared.Models
{
    // BookingRoom.cs
    // Phase 12a — Multi-Room Booking join entity.
    // Each row represents one room reserved under a Booking, with a price snapshot
    // taken at booking time so future Room.PricePerNight changes don't retroactively
    // alter historical totals.
    public class BookingRoom
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public decimal PriceAtBooking { get; set; }
    }
}
