namespace HotelManagementSystem.Shared.Models
{
    // Staff.cs
    public class Staff
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Position { get; set; }
        public DateTime HireDate { get; set; }
    }
}
