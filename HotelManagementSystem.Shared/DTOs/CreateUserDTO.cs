using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Shared.DTOs
{
    public class CreateUserDTO
    {
        public string Username { get; set; } = string.Empty;
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        /// <summary>Claims to assign to the new user (at least one required).</summary>
        public List<ClaimDTO> Claims { get; set; } = new();
        // Optional ID proof fields
        public string? IdProofType { get; set; }
        public string? IdProofNumber { get; set; }
    }
}
