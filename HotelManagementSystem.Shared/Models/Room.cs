namespace HotelManagementSystem.Shared.Models
{
    // Room.cs
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }
        public bool IsAvailable { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<RoomAmenity> RoomAmenities { get; set; }
    }
}
