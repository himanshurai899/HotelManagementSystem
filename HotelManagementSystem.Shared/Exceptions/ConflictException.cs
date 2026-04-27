// ConflictException.cs
namespace HotelManagementSystem.Shared.Exceptions
{
    /// <summary>Thrown when a request conflicts with current state (e.g., duplicate key). Maps to HTTP 409.</summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
