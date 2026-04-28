namespace HotelManagementSystem.Shared.DTOs
{
    public class RebookSuggestionDTO
    {
        // RebookSuggestionDTO.cs
        public int OriginalBookingId { get; set; }
        /// <summary>Ordered list of room IDs from the original booking.</summary>
        public List<int> RoomIds { get; set; } = [];
        /// <summary>Denormalized display string, e.g. "101, 102"</summary>
        public string RoomsSummary { get; set; } = string.Empty;  // To avoid complex object graph
        public DateTime LastStayDate { get; set; }
        /// <summary>Number of nights from the original stay — used to suggest a same-length rebook.</summary>
        public int DurationDays { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
