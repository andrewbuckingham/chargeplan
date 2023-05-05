using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Builder.Templates;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public class UserPricingRepository : ContainerBlobRepository<IEnumerable<PriceAtAbsoluteTimes>>, IUserPricingRepository
{
    public UserPricingRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserPricingProfiles";
}