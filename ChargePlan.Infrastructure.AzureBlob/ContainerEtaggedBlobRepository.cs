using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob;

public abstract class ContainerEtaggedBlobRepository<T> : EtaggedBlobRepository<T>, IEtaggedRepository<T>
{
    protected ContainerEtaggedBlobRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected abstract string ContainerName { get; }
    private string FileName(Guid id) => id + ".json";

    public Task DeleteAsync(Guid id) => base.DeleteBlobAsync(ContainerName.ToLower(), FileName(id));

    public async Task<EtaggedEntity<T>?> GetAsync(Guid id)
    {
        var result = await base.GetBlobAsync(ContainerName.ToLower(), FileName(id));

        if (result == null) return null;

        return new(result.Value.Entity, result.Value.ETag);
    }

    public async Task<EtaggedEntity<T>> UpsertAsync(Guid id, EtaggedEntity<T> entity)
    {
        var result = await base.UpsertBlobAsync(ContainerName.ToLower(), FileName(id), (entity.Entity, entity.ETag));

        return new(result.Entity, result.ETag);
    }
}
