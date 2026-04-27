using Microsoft.AspNetCore.Http;

namespace HotelManagementSystem.API.Models
{
    /// <summary>
    /// Form-bound wrapper for the profile photo upload endpoint.
    /// Declaring IFormFile inside a class (rather than directly as an action parameter)
    /// is required for Swashbuckle to generate the multipart/form-data schema correctly.
    /// </summary>
    public class PhotoUploadRequest
    {
        public IFormFile Photo { get; set; } = null!;
    }
}
