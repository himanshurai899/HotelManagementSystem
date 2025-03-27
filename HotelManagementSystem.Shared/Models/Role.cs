using Microsoft.AspNetCore.Identity;

namespace HotelManagementSystem.Shared.Models
{
    public class Role : IdentityRole<int>
    {
        public ICollection<User> Users { get; set; }
    }
}
