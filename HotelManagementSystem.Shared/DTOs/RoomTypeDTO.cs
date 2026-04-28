namespace HotelManagementSystem.Shared.DTOs
{
    public class RoomTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal BasePrice { get; set; }
        public int Capacity { get; set; }
        public int TenantId { get; set; }
        public string? TenantName { get; set; } // To avoid complex object graph
    }
}
