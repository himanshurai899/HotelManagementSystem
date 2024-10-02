namespace HotelManagementSystem.Shared.Models
{
    // RoomType.cs
    public class RoomType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal BasePrice { get; set; }
        public int Capacity { get; set; }
        public ICollection<Room> Rooms { get; set; }
    }
}
