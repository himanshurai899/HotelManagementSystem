namespace HotelManagementSystem.Shared.DTOs
{
    public class RoomDTO
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } // To avoid complex object graph
        public bool IsAvailable { get; set; }
    }
}
