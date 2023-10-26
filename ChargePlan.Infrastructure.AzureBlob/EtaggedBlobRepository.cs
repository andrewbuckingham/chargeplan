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

    protected async Task<(T Entity, string ETag)?> GetBlobAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);

        try
        {
            if (await blob.ExistsAsync() == false) return null;

            var etag = (await blob.GetPropertiesAsync())?.Value.ETag.ToString() ?? String.Empty;
            var entity = await JsonSerializer.DeserializeAsync<T>(await blob.OpenReadAsync(), _jsonOptions);

            if (entity == null) return null;

            return (entity, etag);
        }
        catch (Azure.RequestFailedException rfe)
        {
            throw new InfrastructureException($"{rfe.ErrorCode} accessing {containerName} {blobName}", rfe);
        }
    }

    protected async Task<(T Entity, string ETag)> UpsertBlobAsync(string containerName, string blobName, (T Entity, string ETag) entity)
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
        catch (Azure.RequestFailedException rfe)
        {
            if (rfe.Status == (int)HttpStatusCode.PreconditionFailed)
            {
                throw new ConcurrencyException();
            }
            else
            {
                throw new InfrastructureException($"{rfe.ErrorCode} updating {containerName} {blobName}", rfe);
            }
        }
    }

    protected async Task DeleteBlobAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);

        try
        {
            await blob.DeleteAsync();
        }
        catch (Azure.RequestFailedException rfe)
        {
            throw new InfrastructureException($"{rfe.ErrorCode} deleting {containerName} {blobName}", rfe);
        }
    }
}