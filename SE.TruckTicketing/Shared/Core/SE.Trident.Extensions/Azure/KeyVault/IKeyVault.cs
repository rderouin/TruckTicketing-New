using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SE.TridentContrib.Extensions.Azure.KeyVault;

public interface IKeyVault
{
    Task<string> GetSecret(Uri keyVault, string secretName, string version = null, CancellationToken cancellationToken = default);

    Task<string> GetSecret(Uri secretUri, CancellationToken cancellationToken = default);

    Task<X509Certificate2> GetCertificate(Uri keyVault, string certificateName, string version = null, CancellationToken cancellationToken = default);

    Task<X509Certificate2> GetCertificate(Uri certificateUri, CancellationToken cancellationToken = default);
}
