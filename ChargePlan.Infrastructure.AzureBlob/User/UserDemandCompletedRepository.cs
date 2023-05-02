using System.Text.Json;
using Azure.Storage.Blobs;

public class UserDemandCompletedRepository : ContainerBlobRepository<IEnumerable<DemandCompleted>>, IUserDemandCompletedRepository
{
    public UserDemandCompletedRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserDemandCompleted";
}