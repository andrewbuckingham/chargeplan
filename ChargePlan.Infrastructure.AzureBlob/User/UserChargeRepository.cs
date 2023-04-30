using System.Text.Json;
using Azure.Storage.Blobs;

public class UserChargeRepository : ContainerBlobRepository<IEnumerable<PowerAtAbsoluteTimes>>, IUserChargeRepository
{
    public UserChargeRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserChargeProfiles";
}