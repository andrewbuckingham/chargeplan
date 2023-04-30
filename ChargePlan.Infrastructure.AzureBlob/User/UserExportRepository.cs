using Azure.Storage.Blobs;

public class UserExportRepository : ContainerBlobRepository<IEnumerable<PriceAtAbsoluteTimes>>, IUserExportRepository
{
    public UserExportRepository(BlobServiceClient blobServiceClient) : base(blobServiceClient)
    {
    }

    protected override string ContainerName => "UserExportProfiles";

}