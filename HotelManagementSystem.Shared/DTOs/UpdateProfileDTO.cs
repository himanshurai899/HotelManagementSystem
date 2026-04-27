namespace HotelManagementSystem.Shared.DTOs
{
    public class UpdateProfileDTO
    {
        public string UserName    { get; set; } = string.Empty;
        public string FirstName   { get; set; } = string.Empty;
        public string LastName    { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
