using System.Net;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ChargePlan.Domain.Exceptions;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob;

public class EtaggedBlobRepository<T>
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public EtaggedBlobRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions)
    {
        _blobServiceClient = blobServiceClient;
        _jsonOptions = jsonOptions;
    }

    public async Task<EtaggedEntity<T>?> GetAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);

        if (await blob.ExistsAsync() == false) return null;

        var etag = (await blob.GetPropertiesAsync())?.Value.ETag.ToString() ?? String.Empty;

        // using (var sr = new StreamReader(await blob.OpenReadAsync()))
        // {
        //     string wibble = sr.ReadToEnd();
        // }

        var entity = await JsonSerializer.DeserializeAsync<T>(await blob.OpenReadAsync(), _jsonOptions);

        if (entity == null) return null;

        return new(entity, etag);
    }

    public async Task<EtaggedEntity<T>> UpsertAsync(string containerName, string blobName, EtaggedEntity<T> entity)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);

        var options = new BlobOpenWriteOptions()
        {
            OpenConditions = new() { IfMatch = new Azure.ETag(entity.ETag) }
        };

        try
        {
            using (var stream = await blob.OpenWriteAsync(true, options))
            {
                await JsonSerializer.SerializeAsync(stream, entity.Entity, _jsonOptions);
            }
            var newEtag = (await blob.GetPropertiesAsync())?.Value.ETag.ToString() ?? String.Empty;
            return entity with { ETag = newEtag };
        }
        catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.PreconditionFailed)
        {
            throw new ConcurrencyException();
        }
    }

    public async Task DeleteAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteAsync();
    }
}