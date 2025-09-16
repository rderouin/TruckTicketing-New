using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Humanizer;

using Newtonsoft.Json.Linq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Azure.KeyVault;

using Trident.Logging;

namespace SE.BillingService.Domain.InvoiceDelivery.Transport;

public class InvoiceDeliveryTransportStrategy : IInvoiceDeliveryTransportStrategy
{
    private const string KeyVaultPrefix = "InvoiceDelivery";

    private readonly IInvoiceDeliveryTransportBlobStorage _blobStorage;

    private readonly IKeyVault _keyVault;

    private readonly Uri _keyVaultUri;

    private readonly ILog _log;

    public InvoiceDeliveryTransportStrategy(IEnumerable<IInvoiceDeliveryTransport> deliveryTransports, ILog log, IKeyVault keyVault, Uri keyVaultUri, IInvoiceDeliveryTransportBlobStorage blobStorage)
    {
        _log = log;
        _keyVault = keyVault;
        _keyVaultUri = keyVaultUri;
        _blobStorage = blobStorage;
        Transports = deliveryTransports.ToDictionary(t => t.TransportType);
    }

    private Dictionary<InvoiceDeliveryTransportType, IInvoiceDeliveryTransport> Transports { get; }

    public async Task Send(InvoiceDeliveryContext context)
    {
        // fetch secrets
        InterpolateRequest(context);
        var (forInvoices, forAttachments) = await FetchClientCertificates(context.DeliveryConfig);

        // process parts
        var partNumber = 0;
        var now = DateTime.UtcNow;
        foreach (var part in context.EncodedInvoice.Parts)
        {
            partNumber++;

            // skip the attachment if it is not supported
            if (part.IsAttachment && context.DeliveryConfig.MessageAdapterSettings.AcceptsAttachments == false)
            {
                continue;
            }

            // pick the delivery settings
            var deliverySettings = part.IsAttachment
                                       ? context.DeliveryConfig.AttachmentSettings
                                       : context.DeliveryConfig.TransportSettings;

            // ensure there is a URL
            if (!deliverySettings.DestinationEndpointUri.HasText() &&
                (deliverySettings.TransportType == InvoiceDeliveryTransportType.Http ||
                 deliverySettings.TransportType == InvoiceDeliveryTransportType.Sftp))
            {
                _log.Error(messageTemplate: $"'{deliverySettings.TransportType.Humanize()}' transport for '{context.Config.PlatformCode}' requires a destination endpoint.");
                continue;
            }

            // pick the suitable transport type
            if (Transports.TryGetValue(deliverySettings.TransportType, out var transport))
            {
                // auth to send a piece
                var auth = new InvoiceDeliveryTransportInstructions
                {
                    DestinationUri = deliverySettings.TransportType == InvoiceDeliveryTransportType.Http ||
                                     deliverySettings.TransportType == InvoiceDeliveryTransportType.Sftp
                                         ? new(Interpolate(deliverySettings.DestinationEndpointUri, context, part))
                                         : null,
                    ClientId = deliverySettings.ClientId,
                    ClientSecret = deliverySettings.ClientSecret,
                    Certificate = part.IsAttachment ? forAttachments : forInvoices,
                    HttpVerb = deliverySettings.HttpVerb,
                    HttpHeaders = deliverySettings.HttpHeaders, // consider interpolating headers per request
                };

                // wrap the payload if customized
                var hasCustomPayload = deliverySettings.IsCustomPayload && deliverySettings.PayloadTemplate.HasText();
                if (hasCustomPayload)
                {
                    // translate
                    var customPayload = Interpolate(deliverySettings.PayloadTemplate, context, part);

                    // prep a new stream
                    var memoryStream = new MemoryStream();
                    var writer = new StreamWriter(memoryStream);
                    await writer.WriteAsync(customPayload);
                    await writer.FlushAsync();
                    memoryStream.Position = 0;

                    // replace the data stream
                    await part.DataStream.DisposeAsync();
                    part.DataStream = memoryStream;
                    part.ContentType = deliverySettings.ContentType.HasText() ? deliverySettings.ContentType : part.ContentType;
                }

                // save it before submission
                if (!part.IsAttachment || hasCustomPayload)
                {
                    // copy stream
                    var streamToSave = await part.DataStream.Memorize();

                    // reset back to original
                    part.DataStream.Position = 0;

                    // save to blob
                    await _blobStorage.Upload(_blobStorage.DefaultContainerName, $"{now:yyyyMMdd-HHmmss}-{partNumber}-{context.RequestId}", streamToSave);
                }

                // send it
                await transport.Send(part, auth);
            }
            else
            {
                _log.Error(messageTemplate: $"Transport '{deliverySettings.TransportType}' is not supported.");
            }
        }
    }

    private void InterpolateRequest(InvoiceDeliveryContext context)
    {
        // cover both settings
        foreach (var settings in new[] { context.DeliveryConfig.TransportSettings, context.DeliveryConfig.AttachmentSettings })
        {
            // sub vars in the target URL
            if (settings.DestinationEndpointUri.HasText())
            {
                settings.DestinationEndpointUri = Interpolate(settings.DestinationEndpointUri, context, null);
            }

            // process each HTTP header
            foreach (var kvp in settings.HttpHeaders.ToList())
            {
                var key = kvp.Key;
                var value = kvp.Value;
                if (!key.HasText() || !value.HasText())
                {
                    continue;
                }

                // process each match
                settings.HttpHeaders[key] = Interpolate(value, context, null);
            }
        }
    }

    private string Interpolate(string template, InvoiceDeliveryContext context, EncodedInvoicePart part)
    {
        return MustacheParser.Interpolate(template, MatchEvaluator);

        string MatchEvaluator(string type, string value, string option)
        {
            switch (type)
            {
                // item - data sourced from the JObject that represents the part
                case "item":
                    return (part?.Source.SelectToken(value) as JValue)?.Value?.ToString();

                // payload - reference to the entire source request payload
                case "payload":
                    return context.Request.Payload.SelectToken(value) is JValue token ? token.Value?.ToString() : null;

                // secret - use a prefixed secret from the key vault
                case "secret":
                    return _keyVault.GetSecret(_keyVaultUri, $"{KeyVaultPrefix}-{value}", option).GetAwaiter().GetResult();

                // data - reference to the data stream
                case "data":
                    return value?.ToLowerInvariant() switch
                           {
                               // base64 - convert byte[] into base64
                               "base64" when part.DataStream != null => ConvertToBase64(part.DataStream),
                               _ => default,
                           };

                default:
                    return null;
            }

            string ConvertToBase64(Stream stream)
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                stream.Flush();
                memoryStream.Flush();
                var data = memoryStream.ToArray();
                return Convert.ToBase64String(data);
            }
        }
    }

    private async Task<(X509Certificate2 forInvoices, X509Certificate2 forAttachments)> FetchClientCertificates(InvoiceExchangeDeliveryConfigurationEntity deliveryConfiguration)
    {
        // try fetching a cert for the transport
        var forInvoices = await FetchCertificateFromKeyVault(_keyVault, _keyVaultUri, deliveryConfiguration.TransportSettings.Certificate);

        // try fetching a cert for the attachments only if it's configured
        var forAttachments = default(X509Certificate2);
        if (deliveryConfiguration.MessageAdapterSettings.AcceptsAttachments)
        {
            forAttachments = await FetchCertificateFromKeyVault(_keyVault, _keyVaultUri, deliveryConfiguration.AttachmentSettings.Certificate);
        }

        return (forInvoices, forAttachments);

        static async Task<X509Certificate2> FetchCertificateFromKeyVault(IKeyVault keyVault, Uri keyVaultUri, string certificate)
        {
            if (certificate.HasText() == false)
            {
                return null;
            }

            // must match Mustache pattern to fetch the cert
            var (success, type, value, version) = MustacheParser.Match(certificate);
            if (success && type == "certificate")
            {
                // get it from the key-vault
                return await keyVault.GetCertificate(keyVaultUri, $"{KeyVaultPrefix}-{value}", version);
            }

            // no match = no cert
            return null;
        }
    }
}
