using Azure.Storage.Blobs;

public class UserShiftableDemandRepository : ContainerBlobRepository<IEnumerable<PowerAtRelativeTimes>>, IUserShiftableDemandRepository
{
    public UserShiftableDemandRepository(BlobServiceClient blobServiceClient) : base(blobServiceClient)
    {
    }

    protected override string ContainerName => "UserShiftableDemands";
}