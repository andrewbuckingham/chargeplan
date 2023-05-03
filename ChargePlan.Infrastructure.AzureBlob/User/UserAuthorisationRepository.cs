using System.Text.Json;
using Azure.Storage.Blobs;

public class UserAuthorisationRepository : ContainerBlobRepository<UserAuthorisation>, IUserAuthorisationRepository
{
    public UserAuthorisationRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserAuthorisation";
}