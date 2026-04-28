using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.DTOs
{
    public class BookingDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // To avoid complex object graph

        // Phase 12a — Multi-Room Booking:
        // Authoritative room list. POST/PUT must send Rooms; reads echo it.
        public List<BookingRoomDTO> Rooms { get; set; } = new();

        // Legacy single-room fields — derived from Rooms[0] on read for backward compat.
        // Server ignores these on write (Rooms is authoritative).
        public int RoomId { get; set; }                          // To avoid complex object graph
        public string RoomNumber { get; set; } = string.Empty;   // To avoid complex object graph

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; }

        // Phase 12c — Booking types
        public BookingType BookingType { get; set; } = BookingType.FullDay;
        /// <summary>"HH:mm" string (e.g. "14:30"). Required for Hourly; null for all other types.</summary>
        public string? CheckInTime { get; set; }
        /// <summary>"HH:mm" string (e.g. "16:00"). Required for Hourly; null for all other types.</summary>
        public string? CheckOutTime { get; set; }

        public int TenantId { get; set; }
        public string? TenantName { get; set; } // To avoid complex object graph
    }
}
