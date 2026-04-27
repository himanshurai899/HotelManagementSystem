// ValidationException.cs
namespace HotelManagementSystem.Shared.Exceptions
{
    /// <summary>Thrown when a domain rule or input validation fails. Maps to HTTP 400.</summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
