using SE.TridentContrib.Extensions.Azure.Blobs;

namespace SE.Shared.Domain.Infrastructure;

public class SignatureUploadBlobStorage : BlobStorage, ISignatureUploadBlobStorage
{
    public SignatureUploadBlobStorage(string connectionString, string containerName) : base(connectionString, containerName)
    {
    }

    protected override string Prefix => "signatures";
}

public interface ISignatureUploadBlobStorage : IBlobStorage
{
}
