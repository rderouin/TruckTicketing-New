using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Invoices;
using SE.TruckTicketing.Domain.Entities.Invoices;

using Trident.Contracts.Configuration;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("Invoice")]
public class InvoiceProcessor : BaseEntityProcessor<InvoiceMessage>
{
    private readonly IAppSettings _appSettings;

    private readonly IInvoiceManager _invoiceManager;

    public InvoiceProcessor(IInvoiceManager invoiceManager,
                            IAppSettings appSettings)
    {
        _invoiceManager = invoiceManager;
        _appSettings = appSettings;
    }

    public override async Task Process(EntityEnvelopeModel<InvoiceMessage> entityModel)
    {
        // fetch the current invoice
        var originalInvoice = await _invoiceManager.GetById(entityModel.Payload.Id);
        if (originalInvoice is null)
        {
            throw new ArgumentException("Invoice does not exist");
        }

        // update the current invoice's basic info
        originalInvoice.GlInvoiceNumber = entityModel.Payload.GlInvoiceNumber;
        originalInvoice.Attachments = entityModel.Payload.Attachments ?? new();

        // the original PDF document
        var attachment = GetOriginalFoDocument(entityModel.Payload);
        if (attachment is not null)
        {
            await _invoiceManager.MergeInvoiceFiles(new()
            {
                InvoiceId = originalInvoice.Id,
                InvoiceBlob = new()
                {
                    BlobPath = attachment.BlobPath,
                    ContainerName = attachment.ContainerName,
                    ContentType = attachment.ContentType,
                    FileName = attachment.FileName,
                },
            });

            originalInvoice.RequiresPdfRegeneration = false;
        }

        // save the update
        await _invoiceManager.Save(originalInvoice);
    }

    private InvoiceAttachmentEntity GetOriginalFoDocument(InvoiceMessage invoice)
    {
        // no attachments in the message = no entity
        if (invoice.Attachments is null || invoice.Attachments.Count == 0)
        {
            return null;
        }

        // if no invoice pattern defined, take first attachment
        var primaryInvoiceDocumentPattern = _appSettings.GetKeyOrDefault("InvoiceProcessor:PrimaryInvoiceDocumentPattern");
        if (!primaryInvoiceDocumentPattern.HasText())
        {
            return invoice.Attachments.First();
        }

        // find the latest attachment by pattern
        return invoice.Attachments.Where(attachment => Regex.IsMatch(attachment.FileName, primaryInvoiceDocumentPattern)).MaxBy(attachment => attachment.AttachedOn);
    }
}

public class InvoiceMessage
{
    public Guid Id { get; set; }

    public string GlInvoiceNumber { get; set; }

    public List<InvoiceAttachmentEntity> Attachments { get; set; } = new();
}
