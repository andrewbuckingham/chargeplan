using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob;

public abstract class ContainerEtaggedBlobRepository<T> : EtaggedBlobRepository<T>
{
    protected ContainerEtaggedBlobRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected abstract string ContainerName { get; }
    private string FileName(Guid id) => id + ".json";

    public Task DeleteAsync(Guid id) => base.DeleteAsync(ContainerName.ToLower(), FileName(id));

    public Task<EtaggedEntity<T>?> GetAsync(Guid id) => base.GetAsync(ContainerName.ToLower(), FileName(id));

    public Task<EtaggedEntity<T>> UpsertAsync(Guid id, EtaggedEntity<T> entity) => base.UpsertAsync(ContainerName.ToLower(), FileName(id), entity);
}
