using Azure.Storage.Blobs;

public class UserChargeRepository : ContainerBlobRepository<IEnumerable<PowerAtAbsoluteTimes>>, IUserChargeRepository
{
    public UserChargeRepository(BlobServiceClient blobServiceClient) : base(blobServiceClient)
    {
    }

    protected override string ContainerName => "UserChargeProfiles";
}