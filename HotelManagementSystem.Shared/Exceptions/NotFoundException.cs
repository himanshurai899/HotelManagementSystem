// NotFoundException.cs
namespace HotelManagementSystem.Shared.Exceptions
{
    /// <summary>Thrown when a requested resource cannot be located. Maps to HTTP 404.</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
