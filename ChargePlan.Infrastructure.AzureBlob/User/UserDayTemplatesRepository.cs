using System.Text.Json;
using Azure.Storage.Blobs;

public class UserDayTemplatesRepository : ContainerBlobRepository<ChargePlanTemplatedParameters>, IUserDayTemplatesRepository
{
    public UserDayTemplatesRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserDayTemplates";
}