using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Pdf;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;

using Trident.Business;
using Trident.Contracts;
using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Mapper;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Invoices;

public interface IInvoiceManager : IManager<Guid, InvoiceEntity>
{
    Task<InvoiceEntity> PostInvoiceAction(PostInvoiceActionRequest request);

    Task<InvoiceEntity> InvoiceAdvanceEmailAction(InvoiceAdvancedEmailRequest emailUpdateRequest);

    Task<Uri> GetAttachmentDownloadUrl(CompositeKey<Guid> invoiceKey, Guid attachmentId);

    Task<(InvoiceAttachmentEntity attachmentEntity, Uri uri)> GetAttachmentUploadUrl(CompositeKey<Guid> invoiceKey, string filename, string contentType);

    Task<InvoiceEntity> MarkFileUploaded(CompositeKey<Guid> invoiceKey, Guid attachmentId);

    Task MergeInvoiceFiles(InvoiceMergeModel mergeModel);

    Task SaveCollectionInfo(InvoiceCollectionModel model);
}

public class InvoiceManager : ManagerBase<Guid, InvoiceEntity>, IInvoiceManager
{
    private const string TruckTicketOnlyAttachmentsFolder = "tt-invoice-attachments";

    private readonly IProvider<Guid, AccountContactIndexEntity> _accountContactProvider;

    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IInvoiceAttachmentsBlobStorage _blobStorage;

    private readonly IEmailTemplateSender _emailTemplateSender;

    private readonly IProvider<Guid, InvoiceConfigurationEntity> _invoiceConfigProvider;

    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly IMapperRegistry _mapperRegistry;

    private readonly IManager<Guid, NoteEntity> _noteManager;

    private readonly IPdfMerger _pdfMerger;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    public InvoiceManager(ILog logger,
                          IProvider<Guid, InvoiceEntity> provider,
                          IProvider<Guid, SalesLineEntity> salesLineProvider,
                          IInvoiceAttachmentsBlobStorage blobStorage,
                          IManager<Guid, NoteEntity> noteManager,
                          IUserContextAccessor userContextAccessor,
                          IPdfMerger pdfMerger,
                          IEmailTemplateSender emailTemplateSender,
                          IMapperRegistry mapperRegistry,
                          IProvider<Guid, InvoiceConfigurationEntity> invoiceConfigProvider,
                          IProvider<Guid, AccountContactIndexEntity> accountContactProvider,
                          IProvider<Guid, AccountEntity> accountProvider,
                          IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider,
                          IValidationManager<InvoiceEntity> validationManager = null,
                          IWorkflowManager<InvoiceEntity> workflowManager = null) :
        base(logger, provider, validationManager, workflowManager)
    {
        _salesLineProvider = salesLineProvider;
        _blobStorage = blobStorage;
        _noteManager = noteManager;
        _userContextAccessor = userContextAccessor;
        _pdfMerger = pdfMerger;
        _emailTemplateSender = emailTemplateSender;
        _mapperRegistry = mapperRegistry;
        _invoiceConfigProvider = invoiceConfigProvider;
        _accountContactProvider = accountContactProvider;
        _accountProvider = accountProvider;
        _loadConfirmationProvider = loadConfirmationProvider;
    }

    public async Task<InvoiceEntity> PostInvoiceAction(PostInvoiceActionRequest request)
    {
        var invoice = await Provider.GetById(request.InvoiceKey); // PK - OK

        if (invoice.Status != request.InvoiceStatus)
        {
            // only save invoice when there's a status change or reversal requested
            invoice.Status = request.InvoiceStatus;

            if (request.InvoiceStatus == InvoiceStatus.AgingUnSent)
            {
                invoice.TransactionComplete = false;
            }

            if (request.InvoiceStatus == InvoiceStatus.Posted)
            {
                invoice.TransactionComplete = true;
                await SetInvoiceDistributionStatus(invoice);
            }

            await Save(invoice);

            await AddNote(invoice.Id, $"Invoice status changed to {request.InvoiceStatus}.");
        }

        // Note, EntityPublishMessageTask used to publish invoice message

        if (request.InvoiceAction != default)
        {
            await ProcessAction(request.InvoiceAction, invoice);
        }

        // saves any outstanding items to the database
        await SaveDeferred();

        return invoice;
    }

    public async Task<InvoiceEntity> InvoiceAdvanceEmailAction(InvoiceAdvancedEmailRequest request)
    {
        var invoice = await Provider.GetById(request.InvoiceKey); // PK - OK

        // email only a posted invoice
        if (invoice.Status != InvoiceStatus.Posted)
        {
            await LogInvoiceWarning(invoice.Id, "Emailing an invoice is allowed only the Posted status.");
            return invoice;
        }

        // email the invoice, in case of error, let UI know
        var message = await EmailPostedInvoice(invoice, request);
        if (message.HasText())
        {
            throw new InvalidOperationException(message);
        }

        await UpdateInvoiceDistributionStatus(invoice, true, request);

        return invoice;
    }

    public async Task<Uri> GetAttachmentDownloadUrl(CompositeKey<Guid> invoiceKey, Guid attachmentId)
    {
        var invoice = await Provider.GetById(invoiceKey); // PK - OK
        var attachment = invoice?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return null;
        }

        return _blobStorage.GetDownloadUri(attachment.ContainerName, attachment.BlobPath, $"attachment; filename=\"{attachment.FileName}\"", attachment.ContentType);
    }

    public async Task<(InvoiceAttachmentEntity attachmentEntity, Uri uri)> GetAttachmentUploadUrl(CompositeKey<Guid> invoiceKey, string filename, string contentType)
    {
        // generate URL
        var attachmentId = Guid.NewGuid();
        var blobPath = $"{TruckTicketOnlyAttachmentsFolder}/{attachmentId}";
        var uri = _blobStorage.GetUploadUri(_blobStorage.DefaultContainerName, blobPath);

        // the draft attachment entity
        var invoiceAttachmentEntity = new InvoiceAttachmentEntity
        {
            Id = attachmentId,
            FileName = filename,
            ContentType = contentType,
            AttachedOn = DateTimeOffset.UtcNow,
            IsUploaded = false,
            BlobPath = blobPath,
            ContainerName = _blobStorage.DefaultContainerName,
        };

        // save it
        var invoice = await Provider.GetById(invoiceKey); // PK - OK
        invoice.Attachments.Add(invoiceAttachmentEntity);
        await Provider.Update(invoice);

        return (invoiceAttachmentEntity, uri);
    }

    public async Task<InvoiceEntity> MarkFileUploaded(CompositeKey<Guid> invoiceKey, Guid attachmentId)
    {
        var invoice = await Provider.GetById(invoiceKey); // PK - OK
        var attachment = invoice?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return invoice;
        }

        // update the flag to uploaded/available
        attachment.IsUploaded = true;
        await Provider.Update(invoice);

        return invoice;
    }

    public async Task MergeInvoiceFiles(InvoiceMergeModel mergeModel)
    {
        // fetch the invoice
        var invoice = await Provider.GetById(mergeModel.InvoiceId); // PK - TODO: INT
        if (invoice == null)
        {
            // the invoice does not exist in the db
            return;
        }

        var invoiceConfig = await _invoiceConfigProvider.GetById(invoice.InvoiceConfigurationId);
        if (invoiceConfig == null || (!invoiceConfig.IncludeExternalDocumentAttachment && !invoiceConfig.IncludeInternalDocumentAttachment))
        {
            return;
        }

        try
        {
            // fetch sales lines that have attachment scans
            var salesLines = (await _salesLineProvider.Get(sl => sl.InvoiceId == invoice.Id)).OrderBy(sl => sl.TruckTicketDate).ThenBy(sl => sl.TruckTicketNumber)
                                                                                             .ToList(); // PK - XP for SL by Invoice ID

            if (!salesLines.Any())
            {
                // no sales lines = nothing to merge, just skip the rest
                return;
            }

            // PDF merging handler
            using var pdfMergingHandler = _pdfMerger.StartMerging();

            // download the generated invoice
            await using var invoiceStream = await _blobStorage.Download(mergeModel.InvoiceBlob.ContainerName, mergeModel.InvoiceBlob.BlobPath);
            pdfMergingHandler.Append(await invoiceStream.Memorize());

            // download approved LC attachments
            var loadConfirmationAttachments = (await _loadConfirmationProvider.Get(lc => lc.InvoiceId == invoice.Id &&
                                                                                         lc.Status != LoadConfirmationStatus.Void))
                                             .OrderBy(lc => lc.Number)
                                             .SelectMany(lc => lc.Attachments)
                                             .Where(attachment => attachment.IsIncludedInInvoice.GetValueOrDefault(false) &&
                                                                  attachment.FileName.EndsWith("pdf", StringComparison.OrdinalIgnoreCase))
                                             .DistinctBy(attachment => attachment.FileName)
                                             .ToList(); // PK - XP for LC by Invoice ID

            foreach (var attachment in loadConfirmationAttachments)
            {
                await using var stream = await _blobStorage.Download(attachment.BlobContainer, attachment.BlobPath);
                pdfMergingHandler.Append(await stream.Memorize());
            }

            // download SL attachments
            var downloadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await ForSortedAttachments(salesLines, async salesLineAttachment =>
                                                   {
                                                       if (salesLineAttachment.File.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                                                           ((salesLineAttachment.IsExternalAttachment() && invoiceConfig.IncludeExternalDocumentAttachment) ||
                                                            (salesLineAttachment.IsInternalAttachment() && invoiceConfig.IncludeInternalDocumentAttachment)) &&
                                                           !downloadedFiles.Contains(salesLineAttachment.File))
                                                       {
                                                           await using var stream = await _blobStorage.Download(salesLineAttachment.Container, salesLineAttachment.Path);
                                                           pdfMergingHandler.Append(await stream.Memorize());
                                                           downloadedFiles.Add(salesLineAttachment.File);
                                                       }
                                                   });

            // upload the new PDF
            await using var mergedDocumentsStream = new MemoryStream();
            pdfMergingHandler.Save(mergedDocumentsStream);
            mergedDocumentsStream.Position = 0;
            await _blobStorage.Upload(mergeModel.InvoiceBlob.ContainerName, mergeModel.InvoiceBlob.BlobPath, mergedDocumentsStream);
        }
        catch (Exception x)
        {
            var nl = Environment.NewLine;
            await AddNote(mergeModel.InvoiceId, $"Failed to merge documents.{nl}{nl}System exception: {x.Message}");
        }
        finally
        {
            // in either case, toggle off the invoice generation flag and save the invoice with the original attachment
            invoice.RequiresPdfRegeneration = false;
        }
    }

    public async Task SaveCollectionInfo(InvoiceCollectionModel model)
    {
        var invoice = await Provider.GetById(model.InvoiceKey); // PK - OK
        if (invoice == null)
        {
            return;
        }

        invoice.CollectionOwner = model.CollectionOwner;
        invoice.CollectionReason = model.CollectionReason;
        invoice.CollectionNotes = model.CollectionNotes;
        // self-heal from blanks
        invoice.Generators = invoice.Generators != null && invoice.Generators.List.Any() ? invoice.Generators : null;

        await Save(invoice);
    }

    internal async Task ForSortedAttachments(IEnumerable<SalesLineEntity> salesLines, Func<SalesLineAttachmentEntity, Task> asyncAction)
    {
        foreach (var salesLine in salesLines)
        {
            foreach (var salesLineAttachment in salesLine.Attachments.OrderBy(GetPrimaryRank).ThenBy(GetSecondaryRank))
            {
                await asyncAction(salesLineAttachment);
            }
        }

        int GetPrimaryRank(SalesLineAttachmentEntity attachmentEntity)
        {
            return attachmentEntity.IsInternalAttachment() ? 1 : attachmentEntity.IsExternalAttachment() ? 2 : 100;
        }

        int GetSecondaryRank(SalesLineAttachmentEntity attachmentEntity)
        {
            return attachmentEntity.IsInternalAttachmentByFile() ? 1 : attachmentEntity.IsExternalAttachmentByFile() ? 2 : 100;
        }
    }

    private Task LogInvoiceWarning(Guid invoiceId, string message)
    {
        Logger.Warning(messageTemplate: message);
        return AddNote(invoiceId, message);
    }

    private async Task ProcessAction(InvoiceAction invoiceAction, InvoiceEntity invoice)
    {
        if (invoiceAction == InvoiceAction.Open)
        {
            // Invoice PDF is opened in client
            // this if condition is here to add additional logic - if needed
        }
        else if (invoiceAction == InvoiceAction.Resend)
        {
            // By updating invoice distribution status, the invoice is saved,
            // triggering the publishing (& therefore resend) of the invoice entity. 
            await UpdateInvoiceDistributionStatus(invoice);
        }
        else if (invoiceAction == InvoiceAction.Email)
        {
            // email only a posted invoice
            if (invoice.Status != InvoiceStatus.Posted)
            {
                await LogInvoiceWarning(invoice.Id, "Emailing an invoice is allowed only the Posted status.");
                return;
            }

            // email the invoice, in case of error, let UI know
            var message = await EmailPostedInvoice(invoice, null);
            if (message.HasText())
            {
                throw new InvalidOperationException(message);
            }

            await UpdateInvoiceDistributionStatus(invoice, true);
        }
        else if (invoiceAction == InvoiceAction.Post)
        {
            // when invoice status is Posted invoice status has changed
            // when status changes EntityPublishMessageTask publishes the message
            // this else condition is here to add additional logic - if needed

            if (invoice.Status is InvoiceStatus.Posted)
            {
                await AddNoteForUpdateInvoiceDistributionStatus(invoice);
            }
        }
        else if (invoiceAction == InvoiceAction.Regenerate)
        {
            invoice.RequiresPdfRegeneration = true;
            await Save(invoice);
        }
    }

    private async Task<string> EmailPostedInvoice(InvoiceEntity invoice, InvoiceAdvancedEmailRequest invoiceEmailUpdateRequest)
    {
        // fetch the the customer
        var customer = await _accountProvider.GetById(invoice.CustomerId);
        if (customer == null)
        {
            var message = $"Unable to find a customer '{invoice.CustomerNumber}' (Id: '{invoice.CustomerId}').";
            await LogInvoiceWarning(invoice.Id, message);
            return message;
        }

        // fetch the billing contact
        var contact = customer.Contacts.FirstOrDefault(c => c.Id == invoice.BillingContactId);
        if (contact == null)
        {
            // fall back to the default billing contact
            contact = customer.Contacts.FirstOrDefault(c => c.IsPrimaryAccountContact);
            if (contact == null)
            {
                var message = $"Unable to find a billing contact to send an invoice for the customer '{invoice.CustomerNumber}' (Id: '{invoice.CustomerId}').";
                await LogInvoiceWarning(invoice.Id, message);
                return message;
            }
        }

        if (!contact.Email.HasText())
        {
            var message = $"The provided billing contact doesn't have the email specified for the account '{invoice.CustomerNumber}' (Id: '{invoice.CustomerId}').";
            await LogInvoiceWarning(invoice.Id, message);
            return message;
        }

        // take the latest invoice PDF
        var attachment = invoice.Attachments.MaxBy(i => i.AttachedOn);
        if (attachment == null)
        {
            var message = $"There are no attachments for the invoice '{invoice.GlInvoiceNumber}' (IP: {invoice.ProformaInvoiceNumber}).";
            await LogInvoiceWarning(invoice.Id, message);
            return message;
        }

        // download the attachment
        await using var stream = await _blobStorage.Download(attachment.ContainerName, attachment.BlobPath);
        var data = await stream.ReadAll();

        // send the email
        await _emailTemplateSender.Dispatch(new()
        {
            TemplateKey = EmailTemplateEventNames.InvoicePaymentRequest,
            Recipients = contact.Email,
            CcRecipients = string.Empty,
            BccRecipients = string.Empty,
            AdHocNote = string.Empty,
            AdHocAttachments = new(),
            ContextBag = new()
            {
                [nameof(Invoice)] = _mapperRegistry.Map<Invoice>(invoice).ToJson(),
                [nameof(InvoiceAttachment)] = _mapperRegistry.Map<InvoiceAttachment>(attachment).ToJson(),
                [nameof(InvoiceAdvancedEmailRequest)] = invoiceEmailUpdateRequest,
                ["PDF"] = data.ToJson(),
            },
        });

        return null;
    }

    private async Task SetInvoiceDistributionStatus(InvoiceEntity invoice, bool isManuallyDistributedByEmail = false)
    {
        var currentUser = _userContextAccessor.UserContext;
        var customer = await _accountProvider.GetById(invoice.CustomerId);
        invoice.DistributionMethod = isManuallyDistributedByEmail
                                         ? InvoiceDistributionMethod.Email
                                         : customer?.BillingType switch
                                         {
                                             BillingType.Email => InvoiceDistributionMethod.Email,
                                             BillingType.Mail => InvoiceDistributionMethod.Mail,
                                             BillingType.CreditCard => InvoiceDistributionMethod.Email,
                                             BillingType.EDIInvoiceTicket => InvoiceDistributionMethod.EDI,
                                             _ => InvoiceDistributionMethod.Unknown,
                                         };

        invoice.LastDistributionDate = DateTimeOffset.UtcNow;
        invoice.LastSentByName = currentUser?.DisplayName;
        Guid.TryParse(currentUser?.ObjectId, out var lastSentById);
        invoice.LastSentById = lastSentById;
    }

    private async Task UpdateInvoiceDistributionStatus(InvoiceEntity invoice, bool isManuallyDistributedByEmail = false, InvoiceAdvancedEmailRequest invoiceEmailUpdateRequest = null)
    {
        await SetInvoiceDistributionStatus(invoice, isManuallyDistributedByEmail);

        await AddNoteForUpdateInvoiceDistributionStatus(invoice, invoiceEmailUpdateRequest);

        await Save(invoice);

    }

    private async Task AddNoteForUpdateInvoiceDistributionStatus(InvoiceEntity invoice, InvoiceAdvancedEmailRequest invoiceEmailUpdateRequest = null)
    {
        var contact = await _accountContactProvider.GetById(invoice.BillingContactId);
        var sentTo = invoice.DistributionMethod switch
        {
            InvoiceDistributionMethod.EDI => "EDI",
            InvoiceDistributionMethod.Mail => "Mail",
            InvoiceDistributionMethod.Unknown => "Unknown",
            _ => $"{contact?.Name} {contact?.Email}",
        };

        var currentUser = _userContextAccessor.UserContext;
        var note = new StringBuilder();
        note.AppendLine("An attempt was made to send this invoice to the customer:");
        note.AppendLine($"Send Date: {DateTimeOffset.UtcNow:g}");
        note.AppendLine($"Send By: {currentUser?.DisplayName}");

        if (invoiceEmailUpdateRequest?.IsCustomeEmail == true)
        {
            note.AppendLine($"Sent To: {(invoiceEmailUpdateRequest.To.HasText() ? invoiceEmailUpdateRequest.To : string.Empty)}");

            note.AppendLine($"Sent Cc: {(invoiceEmailUpdateRequest.Cc.HasText() ? invoiceEmailUpdateRequest.Cc : string.Empty)}");

            note.AppendLine($"Sent Bcc: {(invoiceEmailUpdateRequest.Bcc.HasText() ? invoiceEmailUpdateRequest.Bcc : string.Empty)}");
        }
        else if (sentTo.HasText())
        {
            note.AppendLine($"Sent To: {sentTo}");
        }

        await _noteManager.Save(new()
        {
            Id = Guid.NewGuid(),
            ThreadId = $"{Databases.Discriminators.Invoice}|{invoice.Id}",
            Comment = note.ToString(),
        }, true);
    }

    private async Task AddNote(Guid invoiceId, string text)
    {
        var userContext = _userContextAccessor.UserContext;

        await _noteManager.Save(new()
        {
            Id = Guid.NewGuid(),
            Comment = text,
            NotEditable = true,
            ThreadId = $"{Databases.Discriminators.Invoice}|{invoiceId}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = _userContextAccessor?.UserContext?.DisplayName,
            CreatedBy = _userContextAccessor?.UserContext?.DisplayName,
        });
    }
}
