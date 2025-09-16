using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class TtScannedAttachmentBlobStorage : BlobStorage, ITtScannedAttachmentBlobStorage
{
    public TtScannedAttachmentBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    protected override string Prefix => "attachments";
}

public interface ITtScannedAttachmentBlobStorage : IBlobStorage
{
}
