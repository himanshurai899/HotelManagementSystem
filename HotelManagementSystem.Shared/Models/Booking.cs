using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.Models
{
    // Booking.cs
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        // Phase 12a — Multi-Room Booking:
        // The single RoomId / Room navigation has been replaced by a collection.
        // A booking may now span multiple rooms (e.g., a family taking two adjacent rooms).
        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; }

        // Phase 12c — Booking types
        public BookingType BookingType { get; set; } = BookingType.FullDay;
        /// <summary>Required for Hourly bookings. Stored as SQL time(7).</summary>
        public TimeSpan? CheckInTime { get; set; }
        /// <summary>Required for Hourly bookings. Stored as SQL time(7).</summary>
        public TimeSpan? CheckOutTime { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
