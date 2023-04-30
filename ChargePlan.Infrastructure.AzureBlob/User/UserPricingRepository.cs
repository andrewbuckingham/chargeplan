using Azure.Storage.Blobs;

public class UserPricingRepository : ContainerBlobRepository<IEnumerable<PriceAtAbsoluteTimes>>, IUserPricingRepository
{
    public UserPricingRepository(BlobServiceClient blobServiceClient) : base(blobServiceClient)
    {
    }

    protected override string ContainerName => "UserPricingProfiles";
}