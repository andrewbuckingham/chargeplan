using System.Text.Json;
using Azure.Storage.Blobs;

namespace ChargePlan.Infrastructure.AzureBlob;

public class BlobRepository<T>
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public BlobRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions)
    {
        _blobServiceClient = blobServiceClient;
        _jsonOptions = jsonOptions;
    }

    public async Task<T?> GetAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);

        if (await blob.ExistsAsync() == false) return default(T);

        var entity = await JsonSerializer.DeserializeAsync<T>(await blob.OpenReadAsync(), _jsonOptions);
        return entity;
    }

    public async Task<T> UpsertAsync(string containerName, string blobName, T entity)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);
        using (var stream = await blob.OpenWriteAsync(true))
        {
            await JsonSerializer.SerializeAsync(stream, entity, _jsonOptions);
        }
        return entity;
    }

    public async Task DeleteAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteAsync();
    }
}