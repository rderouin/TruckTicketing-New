using SE.Shared.Domain.Entities.Changes;
using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class ChangeBlobStorage : BlobStorage, IChangeBlobStorage
{
    public ChangeBlobStorage(string connectionString, string defaultContainerName) : base(connectionString, defaultContainerName)
    {
    }

    protected override string Prefix => "changes";
}
