using System.Text.Json;
using Azure.Storage.Blobs;
using ChargePlan.Domain.Solver;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Entities.ForecastTuning;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Infrastructure.AzureBlob.User;

public class ForecastHistoryRepository : ContainerEtaggedBlobRepository<ForecastHistory>, IForecastHistoryRepository
{
    public ForecastHistoryRepository(BlobServiceClient blobServiceClient, JsonSerializerOptions jsonOptions) : base(blobServiceClient, jsonOptions)
    {
    }

    protected override string ContainerName => "ForecastHistory";
}