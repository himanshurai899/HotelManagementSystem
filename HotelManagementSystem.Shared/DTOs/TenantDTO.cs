namespace HotelManagementSystem.Shared.DTOs
{
    public class TenantDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }
        public string Plan { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
