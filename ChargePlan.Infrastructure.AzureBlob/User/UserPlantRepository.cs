using System.Text.Json;
using Azure.Storage.Blobs;

public class UserPlantRepository : ContainerBlobRepository<UserPlantParameters>, IUserPlantRepository
{
    public UserPlantRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserPlantParameters";
}