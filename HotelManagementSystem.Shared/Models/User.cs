using Microsoft.AspNetCore.Identity;

namespace HotelManagementSystem.Shared.Models
{
    public class User : IdentityUser<int>
    {
        private string _userName;

        public override string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                NormalizedUserName = value?.ToUpperInvariant();
            }
        }
        public override string PhoneNumber { get; set; }
        public string? FirstName        { get; set; }
        public string? LastName         { get; set; }
        public string? ProfilePhotoUrl  { get; set; }
        public string? IdProofType      { get; set; }
        public string? IdProofNumber    { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    }
}
