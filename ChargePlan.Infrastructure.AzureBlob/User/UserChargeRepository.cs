using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Builder.Templates;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public class UserChargeRepository : ContainerBlobRepository<IEnumerable<PowerAtAbsoluteTimes>>, IUserChargeRepository
{
    public UserChargeRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserChargeProfiles";
}