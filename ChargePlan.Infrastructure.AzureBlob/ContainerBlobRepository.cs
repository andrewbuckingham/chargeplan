using Azure.Storage.Blobs;

public abstract class ContainerBlobRepository<T> : BlobRepository<T>//, IRepository<T>
{
    protected ContainerBlobRepository(BlobServiceClient blobServiceClient) : base(blobServiceClient)
    {
    }

    protected abstract string ContainerName { get; }
    private string FileName(Guid id) => id + ".json";

    public Task DeleteAsync(Guid id) => base.DeleteAsync(ContainerName, FileName(id));

    public Task<T> GetAsync(Guid id) => base.GetAsync(ContainerName, FileName(id));

    public Task<T> UpsertAsync(Guid id, T entity) => base.UpsertAsync(ContainerName, FileName(id), entity);
}
