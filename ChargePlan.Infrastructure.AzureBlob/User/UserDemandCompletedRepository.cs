using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public class UserDemandCompletedRepository : ContainerBlobRepository<IEnumerable<DemandCompleted>>, IUserDemandCompletedRepository
{
    public UserDemandCompletedRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserDemandCompleted";
}