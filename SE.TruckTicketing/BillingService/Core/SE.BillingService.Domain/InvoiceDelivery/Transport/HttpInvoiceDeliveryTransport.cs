using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.Shared.Common.Constants;
using SE.Shared.Common.Extensions;

using Trident.Logging;

namespace SE.BillingService.Domain.InvoiceDelivery.Transport;

public class HttpInvoiceDeliveryTransport : IInvoiceDeliveryTransport
{
    private static readonly AsyncRetryPolicy<HttpResponseMessage> Policy = HttpPolicyExtensions.HandleTransientHttpError()
                                                                                               .RetryAsync(3);

    private readonly ILog _logger;

    public HttpInvoiceDeliveryTransport(ILog logger)
    {
        _logger = logger;
    }

    public InvoiceDeliveryTransportType TransportType => InvoiceDeliveryTransportType.Http;

    public async Task Send(EncodedInvoicePart part, InvoiceDeliveryTransportInstructions instructions)
    {
        HttpResponseMessage responseMessage = null;
        try
        {
            responseMessage = await Policy.ExecuteAsync(async () =>
                                                        {
                                                            using var client = CreateHttpClient(instructions.Certificate);
                                                            var request = CreateRequest(instructions);
                                                            AddPayload(request, part, _logger);
                                                            return await client.SendAsync(request);
                                                        });

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            if (ContentTypes.StringContentTypes.Contains(responseMessage?.Content?.Headers.ContentType?.MediaType))
            {
                var responseContent = await responseMessage!.Content.ReadAsStringAsync();
                throw new InvoiceDeliveryException("The remote system returned HTTP 400 code (Bad Request).", e)
                {
                    AdditionalMessage = responseContent,
                };
            }

            throw;
        }
    }

    private static HttpRequestMessage CreateRequest(InvoiceDeliveryTransportInstructions instructions)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = instructions.DestinationUri,
            Method = instructions.HttpVerb switch
                     {
                         HttpVerb.Post => HttpMethod.Post,
                         HttpVerb.Put => HttpMethod.Put,
                         HttpVerb.Patch => HttpMethod.Patch,
                         _ => throw new ArgumentOutOfRangeException($"HTTP method '{instructions.HttpVerb}' is not supported."),
                     },
        };

        // append headers
        foreach (var header in instructions.HttpHeaders)
        {
            if (header.Key.HasText())
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        return request;
    }

    private static void AddPayload(HttpRequestMessage httpRequestMessage, EncodedInvoicePart part, ILog logger)
    {
        httpRequestMessage.Content = new PushStreamContent(OnStreamAvailable, MediaTypeHeaderValue.Parse(part.ContentType));

        async Task OnStreamAvailable(Stream stream, HttpContent content, TransportContext context)
        {
            try
            {
                await part.DataStream.CopyToAsync(stream);
            }
            catch (Exception x)
            {
                logger.Error(exception: x, messageTemplate: $"Error occurred during data submission with this message: {x.Message}");
                throw;
            }
            finally
            {
                stream.Close();
            }
        }
    }

    public virtual HttpClient CreateHttpClient(X509Certificate2 certificate)
    {
        var handler = new HttpClientHandler();

        if (certificate == null)
        {
            return new(handler);
        }

        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.SslProtocols = SslProtocols.Tls12;
        handler.ClientCertificates.Add(certificate);

        return new(handler);
    }
}
