using HotelManagementSystem.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManagementSystem.Shared.Models
{
    // User.cs
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Replace PasswordHash with Password
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
