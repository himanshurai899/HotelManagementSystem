namespace HotelManagementSystem.Shared.Models
{
    // RoomAmenity.cs (Junction table for Room and Amenity many-to-many relationship)
    public class RoomAmenity
    {
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public int AmenityId { get; set; }
        public Amenity Amenity { get; set; }
    }
}
