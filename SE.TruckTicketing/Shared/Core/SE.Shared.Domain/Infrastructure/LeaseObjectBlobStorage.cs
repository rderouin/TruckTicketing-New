using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class LeaseObjectBlobStorage : BlobStorage, ILeaseObjectBlobStorage
{
    public LeaseObjectBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    protected override string Prefix { get; }
}

public interface ILeaseObjectBlobStorage : IBlobStorage
{
}
