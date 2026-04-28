namespace HotelManagementSystem.Shared.Enums;

public enum BookingType
{
    // Enums
    FullDay  = 0,   // single overnight (exactly 1 night)
    NightStay = 1,  // classic multi-night stay (no min enforced)
    LongTerm = 2,   // extended stay — no minimum, admin-flagged
    Yearly   = 3,   // exact multiples of 365 days (1 yr, 2 yr, …)
    Hourly   = 4    // same-day, time-bounded; requires AllowHourlyStay + HourlyRate
}
