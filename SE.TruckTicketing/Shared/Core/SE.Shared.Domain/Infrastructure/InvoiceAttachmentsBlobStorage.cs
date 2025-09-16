using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class InvoiceAttachmentsBlobStorage : BlobStorage, IInvoiceAttachmentsBlobStorage
{
    public InvoiceAttachmentsBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    /// <summary>
    ///     A dedicated container for the invoice attachments with ad-hoc paths.
    /// </summary>
    protected override string Prefix => null;
}

public interface IInvoiceAttachmentsBlobStorage : IBlobStorage
{
}
