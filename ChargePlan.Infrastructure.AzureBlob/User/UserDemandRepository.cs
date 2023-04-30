using System.Text.Json;
using Azure.Storage.Blobs;

public class UserDemandRepository : ContainerBlobRepository<IEnumerable<PowerAtAbsoluteTimes>>, IUserDemandRepository
{
    public UserDemandRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserDemandProfiles";
}