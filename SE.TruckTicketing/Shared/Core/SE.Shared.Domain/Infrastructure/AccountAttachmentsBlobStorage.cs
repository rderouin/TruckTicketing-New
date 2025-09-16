using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class AccountAttachmentsBlobStorage : BlobStorage, IAccountAttachmentsBlobStorage
{
    public AccountAttachmentsBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    protected override string Prefix => "attachments";
}

public interface IAccountAttachmentsBlobStorage : IBlobStorage
{
}
