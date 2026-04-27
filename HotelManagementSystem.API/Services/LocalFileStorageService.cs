using HotelManagementSystem.Shared.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace HotelManagementSystem.API.Services
{
    /// <summary>
    /// Saves uploaded files to wwwroot/uploads/ and returns a relative URL ( /uploads/{filename} ).
    /// The API must call app.UseStaticFiles() so these URLs are served directly.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _uploadsRoot;

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            // WebRootPath is null when no wwwroot folder exists yet — fall back gracefully
            var webRoot    = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _uploadsRoot   = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(_uploadsRoot);
        }

        public async Task<string> SaveAsync(Stream fileStream, string fileName, string contentType)
        {
            var ext        = Path.GetExtension(fileName);
            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var fullPath   = Path.Combine(_uploadsRoot, uniqueName);

            await using var fs = File.Create(fullPath);
            await fileStream.CopyToAsync(fs);

            return $"/uploads/{uniqueName}";
        }

        public Task DeleteAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

            var fileName = Path.GetFileName(fileUrl);
            var fullPath = Path.Combine(_uploadsRoot, fileName);
            if (File.Exists(fullPath)) File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}
