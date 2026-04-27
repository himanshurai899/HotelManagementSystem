namespace HotelManagementSystem.Shared.DTOs
{
    public class UserTenantDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; }   // To avoid complex object graph
        public int TenantId { get; set; }
        public string TenantName { get; set; } // To avoid complex object graph
        public string? TenantRole { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
