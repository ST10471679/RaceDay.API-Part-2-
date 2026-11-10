namespace RaceDay.API.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads an image to Azure Blob Storage and returns its public URL.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="folder">Virtual folder, e.g. "banners" or "profiles".</param>
    Task<string> UploadImageAsync(IFormFile file, string folder);
}
