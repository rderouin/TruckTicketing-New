using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class TruckTicketBlobStorage : BlobStorage, ITruckTicketUploadBlobStorage
{
    public TruckTicketBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    protected override string Prefix => null;
}
