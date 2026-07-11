using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pursuit.Application.Interfaces;

namespace Pursuit.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private static readonly BlobClientOptions _clientOptions = new(BlobClientOptions.ServiceVersion.V2024_08_04);

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _connectionString = configuration["AzureBlobSettings:ConnectionString"]!;
        _containerName = configuration["AzureBlobSettings:ContainerName"]!;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var containerClient = new BlobServiceClient(_connectionString, _clientOptions)
            .GetBlobContainerClient(_containerName);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var extension = Path.GetExtension(fileName);
        var blobName = $"{Guid.NewGuid()}{extension}";

        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);

        _logger.LogInformation("Uploaded blob {BlobName} to container {Container}", blobName, _containerName);

        return blobClient.Uri.ToString();
    }

    public async Task<string> GetDownloadUrlAsync(string blobUrl, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var blobUri = new Uri(blobUrl);
        var blobName = blobUri.Segments.Last();

        var blobClient = new BlobServiceClient(_connectionString, _clientOptions)
            .GetBlobContainerClient(_containerName)
            .GetBlobClient(blobName);

        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(expiry));

        _logger.LogInformation("Generated SAS URL for blob {BlobName} expiring in {Expiry}", blobName, expiry);

        return await Task.FromResult(sasUri.ToString());
    }
}