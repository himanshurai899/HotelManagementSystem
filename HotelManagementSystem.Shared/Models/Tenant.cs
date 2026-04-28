using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.Models
{
    // Tenant.cs
    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }   // unique index
        public TenantPlan Plan { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CurrencyCode { get; set; } = "INR";   // ISO 4217
        public string Locale { get; set; } = "en-IN";       // IETF locale tag

        public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    }
}
