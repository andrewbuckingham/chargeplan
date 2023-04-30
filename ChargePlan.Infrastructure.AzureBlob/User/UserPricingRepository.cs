using System.Text.Json;
using Azure.Storage.Blobs;

public class UserPricingRepository : ContainerBlobRepository<IEnumerable<PriceAtAbsoluteTimes>>, IUserPricingRepository
{
    public UserPricingRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserPricingProfiles";
}