using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Serialization;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx;

public class PidxInvoiceDeliveryMessageEncoder : BaseInvoiceDeliveryMessageEncoder
{
    private readonly Dictionary<decimal, IPidxAdapter> _pidxAdapters;

    public PidxInvoiceDeliveryMessageEncoder(IInvoiceAttachmentsBlobStorage storage, IFileCompressorResolver fileCompressorResolver, IEnumerable<IPidxAdapter> pidxAdapters, ILog log, IAppSettings appSettings)
        : base(storage, fileCompressorResolver, log, appSettings)
    {
        _pidxAdapters = pidxAdapters.ToDictionary(c => c.Version);
    }

    public override MessageAdapterType SupportedMessageAdapterType => MessageAdapterType.Pidx;

    public override async Task<EncodedInvoice> EncodeMessage(InvoiceDeliveryContext context)
    {
        // files/requests to send
        var parts = new List<EncodedInvoicePart>();

        // the target invoice exchange can receive attachments
        if (context.DeliveryConfig.MessageAdapterSettings.AcceptsAttachments)
        {
            // encode attachments separately
            parts.AddRange(await FetchAttachments(context.Request.Blobs, context.DeliveryConfig.MessageAdapterSettings));
        }

        // get the converter to get the PIDX object
        if (_pidxAdapters.TryGetValue(context.DeliveryConfig.MessageAdapterVersion, out var pidxAdapter) == false)
        {
            throw new NotSupportedException("PIDX version is not supported.");
        }

        // get the PIDX object
        var pidx = pidxAdapter.ConvertToPidx(context);

        // main PIDX part
        var encodedInvoicePart = new EncodedInvoicePart
        {
            DataStream = new MemoryStream(ConvertToBlob(pidx, pidxAdapter, context.Request.GetMessageType() ?? default)),
            ContentType = MediaTypeNames.Application.Xml,
            IsAttachment = false,
            Source = context.Medium,
            PreferredFileName = "invoice.xml",
        };

        // add PIDX as a last item to send
        parts.Add(encodedInvoicePart);

        // PIDX to send (either an invoice or a field ticket)
        var encodedInvoice = new EncodedInvoice { Parts = parts };

        // if full PIDX - convert into a multipart/mixed with attachments
        if (context.DeliveryConfig.MessageAdapterSettings.AcceptsAttachments &&
            context.DeliveryConfig.MessageAdapterSettings.EmbedAttachments)
        {
            ConvertToFullPidx(encodedInvoice, parts.IndexOf(encodedInvoicePart), context.DeliveryConfig.MessageAdapterVersion);
        }

        return encodedInvoice;
    }

    private void ConvertToFullPidx(EncodedInvoice encodedInvoice, int invoiceIndex, decimal version)
    {
        // entire contents
        var finalContent = new MultipartContent();

        // separate PIDX from attachments
        var invoicePart = encodedInvoice.Parts[invoiceIndex];
        var attachments = encodedInvoice.Parts.Where(p => p != invoicePart).ToList();

        // PIDX goes first
        var streamContent = new StreamContent(invoicePart.DataStream);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(invoicePart.ContentType);
        streamContent.Headers.Add("Content-ID", new[] { $"pidxv{version * 100:###}_invoice_xml" });
        streamContent.Headers.Add("Content-Location", new[] { "RN-Service-Content" });
        finalContent.Add(streamContent);

        // attachments go next
        foreach (var (part, i) in attachments.Select((p, i) => (p, i)))
        {
            // read the original stream and convert data into base64
            using var memStream = new MemoryStream();
            part.DataStream.CopyTo(memStream);
            var base64 = Convert.ToBase64String(memStream.ToArray());

            // create new content
            var stringContent = new StringContent(base64);
            var preferredFileName = part.PreferredFileName ?? $"Attachment{i}";
            stringContent.Headers.Add("Content-ID", new[] { preferredFileName });
            stringContent.Headers.Add("Content-Transfer-Encoding", "base64");
            stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse(part.ContentType);
            stringContent.Headers.ContentDisposition = new("form-data")
            {
                Name = preferredFileName,
                FileName = preferredFileName,
            };

            finalContent.Add(stringContent);
        }

        // combined stream
        var dataStream = new MemoryStream();
        finalContent.CopyTo(dataStream, default, default);
        dataStream.Position = 0;

        // create a single part
        var oldParts = encodedInvoice.Parts;
        encodedInvoice.Parts = new()
        {
            new()
            {
                DataStream = dataStream,
                ContentType = finalContent.Headers.ContentType!.ToString(),
                IsAttachment = false,
                Source = invoicePart.Source,
                PreferredFileName = invoicePart.PreferredFileName,
            },
        };

        // dispose the replaced list
        foreach (var part in oldParts)
        {
            part.Dispose();
        }
    }

    private byte[] ConvertToBlob(object pidxDocument, IPidxAdapter pidxAdapter, MessageType messageType)
    {
        using var stream = new MemoryStream();
        var serializer = new XmlSerializer(pidxDocument.GetType());
        var namespaces = pidxAdapter.GetXmlSerializerNamespaces(messageType);
        serializer.Serialize(stream, pidxDocument, namespaces);
        stream.Flush();
        return stream.ToArray();
    }
}
