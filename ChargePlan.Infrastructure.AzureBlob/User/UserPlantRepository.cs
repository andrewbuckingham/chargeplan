using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public class UserPlantRepository : ContainerBlobRepository<UserPlantParameters>, IUserPlantRepository
{
    public UserPlantRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserPlantParameters";
}