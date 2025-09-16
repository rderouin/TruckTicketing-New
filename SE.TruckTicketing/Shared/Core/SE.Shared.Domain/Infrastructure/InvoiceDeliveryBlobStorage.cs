using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class InvoiceDeliveryTransportBlobStorage : BlobStorage, IInvoiceDeliveryTransportBlobStorage
{
    public InvoiceDeliveryTransportBlobStorage(string connectionString, string defaultContainerName) : base(connectionString, defaultContainerName)
    {
    }

    protected override string Prefix { get; } = "transport-dumps";
}

public interface IInvoiceDeliveryTransportBlobStorage : IBlobStorage
{
}
