using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.Models
{
    // Role.cs
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<User> Users { get; set; }
    }
}
