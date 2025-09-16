using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Shared;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders.Mail;

public class MailMessageInvoiceDeliveryMessageEncoder : BaseInvoiceDeliveryMessageEncoder
{
    public MailMessageInvoiceDeliveryMessageEncoder(IInvoiceAttachmentsBlobStorage storage, IFileCompressorResolver fileCompressorResolver, ILog log, IAppSettings appSettings)
        : base(storage, fileCompressorResolver, log, appSettings)
    {
    }

    public override MessageAdapterType SupportedMessageAdapterType => MessageAdapterType.MailMessage;

    public override async Task<EncodedInvoice> EncodeMessage(InvoiceDeliveryContext context)
    {
        // create a base message object
        var mailMessageSurrogate = context.Medium.ToObject<MailMessageSurrogate>()!;

        // attachments are handled separately
        mailMessageSurrogate.Attachments = new();
        if (context.DeliveryConfig.MessageAdapterSettings.AcceptsAttachments)
        {
            // download all attachments
            var attachmentParts = await FetchAttachments(context.Request.Blobs, context.DeliveryConfig.MessageAdapterSettings);

            // recode them into the object
            foreach (var attachmentPart in attachmentParts)
            {
                mailMessageSurrogate.Attachments.Add(new()
                {
                    Data = await attachmentPart.DataStream.ReadAll(),
                    MediaType = attachmentPart.ContentType,
                    Name = attachmentPart.PreferredFileName,
                });
            }
        }

        // the encoded invoice
        return new()
        {
            Parts = new()
            {
                new()
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mailMessageSurrogate, Formatting.Indented))),
                    ContentType = MediaTypeNames.Application.Json,
                    IsAttachment = false,
                    Source = context.Medium,
                    PreferredFileName = "invoice.json",
                },
            },
        };
    }
}
