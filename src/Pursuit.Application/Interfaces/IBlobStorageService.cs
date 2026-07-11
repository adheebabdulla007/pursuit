namespace Pursuit.Application.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(string blobUrl, TimeSpan expiry, CancellationToken cancellationToken = default);
}