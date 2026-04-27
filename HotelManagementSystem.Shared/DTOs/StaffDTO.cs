namespace HotelManagementSystem.Shared.DTOs
{
    public class StaffDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Position { get; set; }
        public DateTime HireDate { get; set; }
        public int TenantId { get; set; }
        public string TenantName { get; set; } // To avoid complex object graph
    }
}
