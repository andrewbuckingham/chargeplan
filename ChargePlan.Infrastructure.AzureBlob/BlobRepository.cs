using System.Text.Json;
using Azure.Storage.Blobs;

public class BlobRepository<T>
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobRepository(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<T> GetAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        var entity = await JsonSerializer.DeserializeAsync<T>(await blob.OpenReadAsync());
        return entity ?? throw new InvalidOperationException(blobName + " not found");
    }

    public async Task<T> UpsertAsync(string containerName, string blobName, T entity)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        using (var stream = await blob.OpenWriteAsync(true))
        {
            await JsonSerializer.SerializeAsync(stream, entity);
        }
        return entity;
    }

    public async Task DeleteAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteAsync();
    }
}