using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HotelManagementSystem.Shared.Interfaces;

namespace HotelManagementSystem.API.Services
{
    /// <summary>
    /// Uploads files to Azure Blob Storage and returns the public blob URL.
    /// Requires appsettings.json → "AzureStorage:ConnectionString" and "AzureStorage:ContainerName".
    /// Activate by setting "StorageProvider": "AzureBlob" in appsettings.json.
    /// </summary>
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string            _containerName;

        public AzureBlobStorageService(IConfiguration config)
        {
            var connStr = config["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "AzureStorage:ConnectionString is not configured. " +
                    "Add it to appsettings.json or switch StorageProvider to \"Local\".");

            _containerName    = config["AzureStorage:ContainerName"] ?? "profile-photos";
            _blobServiceClient = new BlobServiceClient(connStr);
        }

        public async Task<string> SaveAsync(Stream fileStream, string fileName, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var ext        = Path.GetExtension(fileName);
            var blobName   = $"{Guid.NewGuid():N}{ext}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

            return blobClient.Uri.ToString();
        }

        public async Task DeleteAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return;

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName        = Path.GetFileName(new Uri(fileUrl).LocalPath);

            await containerClient.DeleteBlobIfExistsAsync(blobName);
        }
    }
}
