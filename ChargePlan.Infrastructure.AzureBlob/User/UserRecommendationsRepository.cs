using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public class UserRecommendationsRepository : ContainerBlobRepository<Recommendations>, IUserRecommendationsRepository
{
    public UserRecommendationsRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "UserRecommendationsRepository";
}