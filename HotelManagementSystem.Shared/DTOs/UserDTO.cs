namespace HotelManagementSystem.Shared.DTOs
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // To avoid complex object graph
        public List<string> Roles { get; set; } = new();     // To avoid complex object graph
        public List<ClaimDTO> Claims { get; set; } = new();
    }
}
