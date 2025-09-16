using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

using SE.Shared.Common.Constants;
using SE.Shared.Common.Extensions;
using SE.TridentContrib.Extensions.Azure.KeyVault;

using Trident.Contracts.Configuration;

namespace SE.BillingService.Domain.Integrations.OpenInvoice;

public class OpenInvoiceService : IOpenInvoiceService
{
    public const string KeyVaultCertificateName = "OpenInvoice";

    private static readonly AsyncRetryPolicy<HttpResponseMessage> Policy = HttpPolicyExtensions.HandleTransientHttpError()
                                                                                               .RetryAsync(3);

    private readonly IAppSettings _appSettings;

    private readonly IKeyVault _keyVault;

    public OpenInvoiceService(IKeyVault keyVault, IAppSettings appSettings)
    {
        _keyVault = keyVault;
        _appSettings = appSettings;
    }

    public async Task<OpenInvoiceReceiptsResult> QueryReceiptsAsync(IList<string> receiptNumbers)
    {
        // query to get ticket statuses
        var query = new OpenInvoiceQuery(GetOpenInvoiceUri(), OpenInvoicePath.Receipts)
        {
            Numbers = receiptNumbers,
            Fields = new[] { "receiptNumber", "status", "itemID" },
        };

        // HTTP client cert
        var cert = await GetOpenInvoiceCertificate();

        HttpResponseMessage responseMessage = null;
        try
        {
            // basic retry on transient errors
            responseMessage = await Policy.ExecuteAsync(async () =>
                                                        {
                                                            // http client with a cert
                                                            using var client = CreateHttpClient(cert);

                                                            // query for OI tickets
                                                            var request = new HttpRequestMessage
                                                            {
                                                                Method = HttpMethod.Get,
                                                                RequestUri = query.Build(),
                                                            };

                                                            // execute
                                                            return await client.SendAsync(request);
                                                        });

            // proceed with HTTP 200
            responseMessage.EnsureSuccessStatusCode();

            // parse the response
            var result = await responseMessage.Content.ReadFromJsonAsync<OpenInvoiceReceiptsResult>();
            return result;
        }
        catch (Exception e)
        {
            if (ContentTypes.StringContentTypes.Contains(responseMessage?.Content?.Headers.ContentType?.MediaType))
            {
                var responseContent = await responseMessage!.Content.ReadAsStringAsync();
                var message = $"The remote system returned the following error:{Environment.NewLine}{Environment.NewLine}{responseContent}";
                throw new InvalidOperationException(message, e);
            }

            throw;
        }
    }

    private Uri GetOpenInvoiceUri()
    {
        var host = _appSettings["AppSettings:OpenInvoiceHostUri"];
        if (host == null)
        {
            throw new InvalidOperationException("OpenInvoiceHostUri setting is not configured.");
        }

        return new(host);
    }

    private async Task<X509Certificate2> GetOpenInvoiceCertificate()
    {
        var keyVaultName = _appSettings["Values:InvoiceDeliveryKeyVaultName"];
        if (!keyVaultName.HasText())
        {
            throw new InvalidOperationException("InvoiceDeliveryKeyVaultName setting is not configured.");
        }

        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net");
        return await _keyVault.GetCertificate(keyVaultUri, KeyVaultCertificateName);
    }

    protected virtual HttpClient CreateHttpClient(X509Certificate2 certificate)
    {
        return new(new HttpClientHandler
        {
            SslProtocols = SslProtocols.Tls12,
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ClientCertificates = { certificate },
        });
    }
}
