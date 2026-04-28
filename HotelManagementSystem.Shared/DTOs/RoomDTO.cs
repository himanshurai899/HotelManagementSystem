namespace HotelManagementSystem.Shared.DTOs
{
    public class RoomDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty; // To avoid complex object graph
        public bool IsAvailable { get; set; }
        public bool AllowHourlyStay { get; set; }
        public decimal PricePerNight { get; set; }

        // Phase 12c — Booking type rates (null ⇒ type unavailable for this room)
        public decimal? HourlyRate { get; set; }
        public decimal? DailyRate { get; set; }
        public decimal? MonthlyRate { get; set; }
        public decimal? YearlyRate { get; set; }

        public int TenantId { get; set; }
        public string? TenantName { get; set; } // To avoid complex object graph
    }
}
