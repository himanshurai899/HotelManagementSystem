namespace HotelManagementSystem.Shared.Models
{
    // UserTenant.cs
    public class UserTenant
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; }
        public string? TenantRole { get; set; }   // tenant-scoped role, e.g. "Administrator", "Staff"; null = Customer
        public DateTime JoinedAt { get; set; }
    }
}
