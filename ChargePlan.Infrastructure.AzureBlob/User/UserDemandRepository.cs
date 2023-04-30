using Azure.Storage.Blobs;

public class UserDemandRepository : ContainerBlobRepository<IEnumerable<PowerAtAbsoluteTimes>>, IUserDemandRepository
{
    public UserDemandRepository(BlobServiceClient blobServiceClient) : base(blobServiceClient)
    {
    }

    protected override string ContainerName => "UserDemandProfiles";
}