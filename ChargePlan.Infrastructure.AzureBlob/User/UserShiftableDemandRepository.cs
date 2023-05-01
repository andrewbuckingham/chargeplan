using System.Text.Json;
using Azure.Storage.Blobs;

public class UserShiftableDemandRepository : ContainerBlobRepository<IEnumerable<PowerAtRelativeTimes>>, IUserShiftableDemandRepository
{
    public UserShiftableDemandRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserShiftableDemands";
}