using System.Text.Json;
using Azure.Storage.Blobs;

public class UserExportRepository : ContainerBlobRepository<IEnumerable<PriceAtAbsoluteTimes>>, IUserExportRepository
{
    public UserExportRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserExportProfiles";

}