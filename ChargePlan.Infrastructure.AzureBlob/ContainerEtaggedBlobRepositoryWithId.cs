using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob;

public abstract class ContainerEtaggedBlobRepositoryWithId<T> : EtaggedBlobRepository<T>, IEtaggedRepositoryWithId<T>
{
    protected ContainerEtaggedBlobRepositoryWithId(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected abstract string ContainerName { get; }
    private string FileName(Guid userId, string id) => $"{userId}-{id}.json";

    public Task DeleteAsync(Guid userId, string id) => base.DeleteBlobAsync(ContainerName.ToLower(), FileName(userId, id));

    public async Task<EtaggedEntityWithId<T>?> GetAsync(Guid userId, string id)
    {
        var result = await base.GetBlobAsync(ContainerName.ToLower(), FileName(userId, id));

        if (result == null) return null;

        return new(result.Value.Entity, result.Value.ETag, id);
    }

    public async Task<EtaggedEntityWithId<T>> UpsertAsync(Guid userId, EtaggedEntityWithId<T> entity)
    {
        var result = await base.UpsertBlobAsync(ContainerName.ToLower(), FileName(userId, entity.Id), (entity.Entity, entity.ETag));

        return new(result.Entity, result.ETag, entity.Id);
    }
}
