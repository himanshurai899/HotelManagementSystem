namespace HotelManagementSystem.Shared.Models
{
    // Room.cs
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; }
        public bool IsAvailable { get; set; }
        public bool AllowHourlyStay { get; set; }
        public decimal PricePerNight { get; set; } = 0;

        // Phase 12c — Booking types (Option C: null rate ⇒ type unavailable for this room).
        public decimal? HourlyRate { get; set; }    // required for Hourly bookings
        public decimal? DailyRate { get; set; }     // forward-compat for FullDay (Phase 17)
        public decimal? MonthlyRate { get; set; }   // forward-compat for LongTerm (Phase 17)
        public decimal? YearlyRate { get; set; }    // forward-compat for Yearly (Phase 17)

        // Phase 12a — Multi-Room Booking: direct Booking ↔ Room link replaced by BookingRoom join.
        // Reverse lookup is via BookingRoom.RoomId.
        public ICollection<RoomAmenity> RoomAmenities { get; set; }
    }
}
