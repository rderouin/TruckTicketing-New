using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.TruckTicketing.Domain.Infrastructure;

public class TradeAgreementUploadBlobStorage : BlobStorage, ITradeAgreementUploadBlobStorage
{
    public TradeAgreementUploadBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    protected override string Prefix { get; }
}

public interface ITradeAgreementUploadBlobStorage : IBlobStorage
{
}
