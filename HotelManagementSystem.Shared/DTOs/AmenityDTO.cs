namespace HotelManagementSystem.Shared.DTOs
{
    public class AmenityDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TenantId { get; set; }
        public string? TenantName { get; set; } // To avoid complex object graph
    }
}
