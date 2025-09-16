using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace SE.TridentContrib.Extensions.Azure.KeyVault;

public class KeyVault : IKeyVault
{
    public async Task<string> GetSecret(Uri keyVault, string secretName, string version = null, CancellationToken cancellationToken = default)
    {
        var client = new SecretClient(keyVault, new DefaultAzureCredential());
        var response = await client.GetSecretAsync(secretName, version, cancellationToken);
        return response.Value.Value;
    }

    public async Task<string> GetSecret(Uri secretUri, CancellationToken cancellationToken = default)
    {
        // parse the full path
        var (keyVault, type, name, version) = ParseKeyVaultPath(secretUri);
        if (type != "secrets")
        {
            return null;
        }

        return await GetSecret(keyVault, name, version, cancellationToken);
    }

    public async Task<X509Certificate2> GetCertificate(Uri keyVault, string certificateName, string version = null, CancellationToken cancellationToken = default)
    {
        var client = new CertificateClient(keyVault, new DefaultAzureCredential());
        var response = await client.DownloadCertificateAsync(certificateName, version, cancellationToken);
        return response.Value;
    }

    public async Task<X509Certificate2> GetCertificate(Uri certificateUri, CancellationToken cancellationToken = default)
    {
        // parse the full path
        var (keyVault, type, name, version) = ParseKeyVaultPath(certificateUri);
        if (type != "certificates")
        {
            return null;
        }

        return await GetCertificate(keyVault, name, version, cancellationToken);
    }

    private static (Uri keyVault, string type, string name, string version) ParseKeyVaultPath(Uri uri)
    {
        var keyVault = $"{uri.Scheme}://{uri.Host}";
        var type = default(string);
        var name = default(string);
        var version = default(string);

        // NOTE: info regarding the URI segments
        // 0 = /
        // 1 = secrets/
        // 2 = SecretName/
        // 3 = 649bf01a129b47c288aac1f8a67ec4af

        if (uri.Segments.Length > 1)
        {
            type = uri.Segments[1].TrimEnd('/');
        }

        if (uri.Segments.Length > 2)
        {
            name = uri.Segments[2].TrimEnd('/');
        }

        if (uri.Segments.Length > 3)
        {
            version = uri.Segments[3].TrimEnd('/');
        }

        return (new(keyVault), type, name, version);
    }
}
