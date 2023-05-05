using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public class UserAuthorisationRepository : ContainerBlobRepository<UserAuthorisation>, IUserAuthorisationRepository
{
    public UserAuthorisationRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserAuthorisation";
}