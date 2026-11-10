using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public class BlobStorageService : IBlobStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"]  = ".png",
        ["image/gif"]  = ".gif",
        ["image/webp"] = ".webp"
    };

    private readonly IConfiguration _configuration;
    private BlobContainerClient? _container;

    public BlobStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Created lazily so viewing a profile still works when Azure isn't configured;
    // only an actual upload needs the connection string.
    private BlobContainerClient GetContainer()
    {
        if (_container != null) return _container;

        var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AzureBlobStorage:ConnectionString is not configured. " +
                "Set it with 'dotnet user-secrets' or the AzureBlobStorage__ConnectionString environment variable.");
        }

        _container = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
        return _container;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was uploaded.");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException("Image must be 5 MB or smaller.");

        if (!AllowedTypes.TryGetValue(file.ContentType, out var extension))
            throw new ArgumentException("Only JPG, PNG, GIF or WEBP images are allowed.");

        // Public read access on blobs so <img src="..."> works in the browser.
        // (The storage account must allow public blob access.)
        var container = GetContainer();
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        });

        return blob.Uri.ToString();
    }
}
