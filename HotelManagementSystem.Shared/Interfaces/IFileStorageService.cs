namespace HotelManagementSystem.Shared.Interfaces
{
    /// <summary>
    /// Abstraction for file persistence.
    /// Implementations: LocalFileStorageService (default) and AzureBlobStorageService.
    /// Configured via appsettings.json → "StorageProvider": "Local" | "AzureBlob"
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>Persists the file stream and returns a publicly accessible URL.</summary>
        Task<string> SaveAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>Deletes a previously stored file by its URL. No-ops gracefully.</summary>
        Task DeleteAsync(string fileUrl);
    }
}
