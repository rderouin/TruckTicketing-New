using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using JetBrains.Annotations;

using Newtonsoft.Json.Linq;

using SE.Enterprise.Contracts.Models;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Enterprise.Contracts.Models.InvoiceDelivery.PayloadModels;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.EmailTemplates.EmailProcessors.LoadConfirmation;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.InvoiceDelivery;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.Shared.Domain.LegalEntity;
using SE.TridentContrib.Extensions.Azure.Blobs;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

using Trident;
using Trident.Contracts;
using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Logging;

// ReSharper disable MemberCanBeProtected.Global

namespace SE.Shared.Domain.Entities.LoadConfirmation;

[UsedImplicitly]
public class LoadConfirmationApprovalWorkflow : ILoadConfirmationApprovalWorkflow
{
    private static readonly List<string> AllowedExtensions = new()
    {
        ".pdf",
        ".jpeg",
        ".png",
        ".tiff",
    };

    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly IBlobStorage _blobStorage;

    private readonly IEmailTemplateSender _emailTemplateSender;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, InvoiceConfigurationEntity> _invoiceConfigurationProvider;

    private readonly IInvoiceDeliveryServiceBus _invoiceDeliveryServiceBus;

    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly IManager<Guid, LoadConfirmationEntity> _loadConfirmationManager;

    private readonly ILog _logger;

    private readonly IManager<Guid, NoteEntity> _noteManager;

    private readonly ILoadConfirmationPdfRenderer _pdfRenderer;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    public LoadConfirmationApprovalWorkflow(ILog logger,
                                            IManager<Guid, LoadConfirmationEntity> loadConfirmationManager,
                                            IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                                            IProvider<Guid, InvoiceConfigurationEntity> invoiceConfigurationProvider,
                                            IProvider<Guid, InvoiceEntity> invoiceProvider,
                                            IProvider<Guid, SalesLineEntity> salesLineProvider,
                                            IProvider<Guid, FacilityEntity> facilityProvider,
                                            IManager<Guid, NoteEntity> noteManager,
                                            IInvoiceDeliveryServiceBus invoiceDeliveryServiceBus,
                                            ILoadConfirmationPdfRenderer pdfRenderer,
                                            IInvoiceAttachmentsBlobStorage blobStorage,
                                            IEmailTemplateSender emailTemplateSender,
                                            IProvider<Guid, AccountEntity> accountProvider,
                                            IProvider<Guid, LegalEntityEntity> legalEntityProvider,
                                            IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                            IUserContextAccessor userContextAccessor = null)
    {
        _logger = logger;
        _loadConfirmationManager = loadConfirmationManager;
        _billingConfigurationProvider = billingConfigurationProvider;
        _invoiceConfigurationProvider = invoiceConfigurationProvider;
        _invoiceProvider = invoiceProvider;
        _salesLineProvider = salesLineProvider;
        _facilityProvider = facilityProvider;
        _noteManager = noteManager;
        _invoiceDeliveryServiceBus = invoiceDeliveryServiceBus;
        _pdfRenderer = pdfRenderer;
        _blobStorage = blobStorage;
        _emailTemplateSender = emailTemplateSender;
        _accountProvider = accountProvider;
        _legalEntityProvider = legalEntityProvider;
        _truckTicketProvider = truckTicketProvider;
        _userContextAccessor = userContextAccessor;
    }

    public async Task DoLoadConfirmationAction(LoadConfirmationSingleRequest requestModel)
    {
        // fetch the load confirmation and validate if it's allowed to be executed
        var lc = await _loadConfirmationManager.GetById(requestModel.LoadConfirmationKey); // PK - OK
        if (!LoadConfirmationTransitions.IsAllowed(lc.Status, requestModel.Action))
        {
            // the status of the current load confirmation doesn't allow to execute the desired action
            return;
        }

        // pick an action to execute
        Func<LoadConfirmationEntity, string, Task> actionMethod =
            requestModel.Action switch
            {
                LoadConfirmationAction.ResendLoadConfirmationSignatureEmail => (l, c) => SendForApprovalUnconditionally(l, null),
                LoadConfirmationAction.ResendAdvancedLoadConfirmationSignatureEmail => (l, c) => SendForApprovalUnconditionally(lc, LoadConfirmationAdvancedEmailModel.FromRequest(requestModel)),
                LoadConfirmationAction.ResendFieldTickets => (l, c) => DeliverFieldTicketAsync(l),
                LoadConfirmationAction.ApproveSignature => ApproveSignature,
                LoadConfirmationAction.RejectSignature => RejectSignature,
                LoadConfirmationAction.MarkLoadConfirmationAsReady => MarkLoadConfirmationReady,
                LoadConfirmationAction.VoidLoadConfirmation => VoidLoadConfirmation,
                _ => throw new NotSupportedException($"The Load Confirmation Action '{requestModel.Action}' is not supported."),
            };

        // execute
        await actionMethod(lc, requestModel.AdditionalNotes);
        await _loadConfirmationManager.Save(lc);
    }

    public async Task StartFromBeginning(CompositeKey<Guid> loadConfirmationKey, string additionalNotes, bool ignoreCurrentStatus)
    {
        // load the LC
        var loadConfirmation = await _loadConfirmationManager.GetById(loadConfirmationKey); // PK - OK
        if (loadConfirmation == null)
        {
            throw new InvalidOperationException($"Unable to find the Load Confirmation for submission ({loadConfirmationKey}).");
        }

        // validate the LC
        var validationErrors = ValidateLoadConfirmation(loadConfirmation, ignoreCurrentStatus);
        if (validationErrors.Any())
        {
            _logger.Warning(messageTemplate: $"Load confirmation validation errors: {string.Join(Environment.NewLine, validationErrors)}");
            return;
        }

        // reset the gateway flag if upon sending or a manual retry, it will be set to 'true' in case of IEG errors
        loadConfirmation.HasFailedDueToGatewayError = false;

        // add an extra note in case if it's not started with the 'Open' status
        if (additionalNotes.HasText())
        {
            await AddNote(loadConfirmation.Id, additionalNotes, false);
        }

        // Void LC if it has no sales lines as there's nothing to confirm
        var salesLines = (await _salesLineProvider.Get(salesLine => salesLine.LoadConfirmationId == loadConfirmationKey.Id)).ToList(); // PK - XP for SL by LC ID
        if (!salesLines.Any())
        {
            loadConfirmation.Status = LoadConfirmationStatus.Void;
        }
        else
        {
            // workflow branching
            if (loadConfirmation.IsSignatureRequired)
            {
                // send an email for approval
                await SendForApprovalAsync(loadConfirmation, salesLines, null);
            }
            else
            {
                if (loadConfirmation.FieldTicketsUploadEnabled)
                {
                    // field tickets enabled => send LC
                    await DeliverFieldTicketAsync(loadConfirmation);
                }
                else
                {
                    // field tickets are not enabled
                    await SetWaitingForInvoiceAsync(loadConfirmation);
                }
            }
        }

        // save all the changes done to the LC
        await _loadConfirmationManager.Update(loadConfirmation);
    }

    public async Task ContinueFromApprovalEmail(LoadConfirmationApprovalEmail approvalEmail)
    {
        // try parse the modernized token
        var hashStrategy = LoadConfirmationHashStrategy.Version2L6;
        var (lcNumber, lcHash, isSuccessfulParse) = LoadConfirmationTokenFormatter.Parse(approvalEmail.Subject, hashStrategy);

        // v2 token parsing failed, try v1 - legacy
        if (!isSuccessfulParse)
        {
            // update the existing vars
            hashStrategy = LoadConfirmationHashStrategy.Version1L16;
            (lcNumber, lcHash, isSuccessfulParse) = LoadConfirmationTokenFormatter.Parse(approvalEmail.Subject, hashStrategy);
        }

        // v2 & v1 have failed, escape
        if (!isSuccessfulParse)
        {
            // gibberish, the email subject doesn't have a tracking code
            return;
        }

        // load the LC by its number
        var loadConfirmation = (await _loadConfirmationManager.Get(lc => lc.Number == lcNumber)).FirstOrDefault(); // PK - XP for LC by LC Number
        if (loadConfirmation == null)
        {
            return;
        }

        // no need to run this workflow if the LC is already beyond the approval email stage 
        if (loadConfirmation.Status == LoadConfirmationStatus.WaitingForInvoice)
        {
            return;
        }

        // validate the incoming email
        var isEmailHashMatched = ValidateApprovalEmailAsync(loadConfirmation, lcHash, hashStrategy);
        if (isEmailHashMatched == null)
        {
            // illegible = no action / ignore
            return;
        }

        // process emails with a valid hash
        if (isEmailHashMatched == true)
        {
            // process email
            await SaveAttachmentsAndAddNoteAsync(loadConfirmation, approvalEmail);

            // different outcomes for emails with encrypted attachments
            if (approvalEmail.HasEncryptedAttachments)
            {
                // reject with a note
                await RejectAndAddNoteAsync(loadConfirmation, approvalEmail);
            }
            else
            {
                // all is OK, update status
                await SetWaitingSignatureValidationAsync(loadConfirmation);
            }
        }
        else
        {
            // the hash doesn't match
            await ForwardTamperedEmailToFacilityAsync(loadConfirmation, approvalEmail);
        }

        // save all the changes done to the LC
        await _loadConfirmationManager.Update(loadConfirmation);
    }

    public async Task ProcessResponseFromInvoiceGateway(DeliveryResponse fieldTicketResponse)
    {
        // validate the input - this has to be a Load Confirmation response with ID
        if (fieldTicketResponse?.EnterpriseId == null ||
            fieldTicketResponse.GetMessageType() != MessageType.FieldTicketResponse)
        {
            return;
        }

        // load the LC
        var lc = await _loadConfirmationManager.GetById(fieldTicketResponse.EnterpriseId); // PK - TODO: INT
        if (lc == null)
        {
            return;
        }

        // process this message only when it's submitted to the invoice exchange gateway
        if (lc.Status != LoadConfirmationStatus.SubmittedToGateway)
        {
            return;
        }

        try
        {
            // keep track of the latest submission to IEG
            lc.HasFailedDueToGatewayError = !fieldTicketResponse.Payload.IsSuccessful;

            // if the submission to IEG has failed, reject the LC
            if (fieldTicketResponse.Payload.IsSuccessful == false)
            {
                // log the error
                var errorMessages = string.Join(Environment.NewLine, fieldTicketResponse.Payload.Message, fieldTicketResponse.Payload.AdditionalMessage);
                _logger.Warning(messageTemplate: $"Response from the Billing Service for the Load Confirmation '{lc.Number}' has errors: {errorMessages}");

                // if it's not a status update, but rather the LC submission, then update the LC status and add notes
                if (fieldTicketResponse.Payload.IsStatusUpdate == false)
                {
                    // set Rejected status for this LC
                    lc.Status = LoadConfirmationStatus.Rejected;

                    // add notes why this happened
                    var notes = new StringBuilder();
                    notes.AppendLine($"Rejected due to an error: {fieldTicketResponse.Payload.Message}");

                    // add extra info
                    if (fieldTicketResponse.Payload.AdditionalMessage.HasText())
                    {
                        // user-friendly response from OI
                        var parsedMessage = TryParseXmlResponse(fieldTicketResponse.Payload.AdditionalMessage);
                        if (parsedMessage.HasText())
                        {
                            notes.AppendLine();
                            notes.AppendLine(parsedMessage);
                        }

                        // original text
                        notes.AppendLine();
                        notes.AppendLine("Remote reply:");
                        notes.AppendLine(fieldTicketResponse.Payload.AdditionalMessage);
                    }

                    await AddNote(lc.Id, notes.ToString(), true);
                }

                // on any unsuccessful message - stop further processing
                return;
            }

            // if a status update comes from the BS/IEG, update/advance the workflow only on valid updates
            // if the response is not a status update, but merely a confirmation that IEG accepted the request, the LC status should stay in the 'Submitted to Gateway'
            if (fieldTicketResponse.Payload.IsStatusUpdate)
            {
                switch (fieldTicketResponse.Payload.RemoteStatus)
                {
                    // happy-path = advance the workflow
                    case RemoteStatus.Approved:
                        lc.Status = LoadConfirmationStatus.WaitingForInvoice;
                        await AddNote(lc.Id, "Approval has been received from the remote invoice gateway.", true);
                        break;

                    // the LC/FT is denied via the IEG updates, LC needs to be corrected/updated... set to rejected with a note
                    case RemoteStatus.Denied:
                        lc.Status = LoadConfirmationStatus.Rejected;
                        await AddNote(lc.Id, "Rejection has been received from the remote invoice gateway.", true);
                        break;
                }
            }
            else
            {
                // if there is no IEG FT configuration for this customer or the provider doesn't support updates, then advance the workflow
                if (fieldTicketResponse.Payload.IsFieldTicketStatusUpdatesSupported != true)
                {
                    lc.Status = LoadConfirmationStatus.WaitingForInvoice;
                    await AddNote(lc.Id, "Marking the load confirmation as Ready as no platforms support status updates.", true);
                }
            }
        }
        finally
        {
            // save the progress
            await _loadConfirmationManager.Update(lc);
        }

        static string TryParseXmlResponse(string xml)
        {
            try
            {
                // no message = no parsing
                if (!xml.HasText())
                {
                    return string.Empty;
                }

                // try parsing the xml doc
                var xDocument = XDocument.Parse(xml);

                // define the prefix for the default namespace
                var xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
                var defaultNamespace = xDocument.Root!.GetDefaultNamespace();
                xmlNamespaceManager.AddNamespace("r", defaultNamespace.NamespaceName);

                // fetch warnings and errors
                var errors = xDocument.XPathSelectElements("//r:Error", xmlNamespaceManager).ToList();
                var warnings = xDocument.XPathSelectElements("//r:Warning", xmlNamespaceManager).ToList();

                // format the message
                var sb = new StringBuilder();

                if (errors.Any())
                {
                    sb.AppendLine("Errors:");
                    foreach (var error in errors.Select(e => e.Value))
                    {
                        sb.AppendLine($"=> {error}");
                    }
                }

                if (warnings.Any())
                {
                    if (errors.Any())
                    {
                        sb.AppendLine();
                    }

                    sb.AppendLine("Warnings:");
                    foreach (var warning in warnings.Select(e => e.Value))
                    {
                        sb.AppendLine($"=> {warning}");
                    }
                }

                return sb.ToString();
            }
            catch
            {
                // NOTE: this is only an attempt to read a predefined XML structure - this is not a critical flow
                return string.Empty;
            }
        }
    }

    public async Task<Uri> FetchLatestDocument(CompositeKey<Guid> loadConfirmationKey, string contentDisposition)
    {
        // load the LC
        var lc = await _loadConfirmationManager.GetById(loadConfirmationKey); // PK - OK
        if (lc == null)
        {
            return null;
        }

        // ensure the latest document exists for a preview
        var (isRegenerated, _, _, _) = await GenerateDocument(lc);
        if (isRegenerated)
        {
            // save the new attachment if it has been generated
            await _loadConfirmationManager.Update(lc);
        }

        // pick the document from attachment that is suitable as the latest document
        var latestDoc = lc.Attachments.FirstOrDefault(e => e.Id == lc.LastGeneratedDocumentId);
        if (latestDoc == null)
        {
            return null;
        }

        // generate a signed SAS-based URL
        var downloadUri = _blobStorage.GetDownloadUri(latestDoc.BlobContainer, latestDoc.BlobPath, $"{contentDisposition}; filename=\"{latestDoc.FileName}\"", latestDoc.ContentType);
        return downloadUri;
    }

    public async Task SendForApprovalUnconditionally(LoadConfirmationEntity loadConfirmation, LoadConfirmationAdvancedEmailModel request)
    {
        var salesLines = (await _salesLineProvider.Get(salesLine => salesLine.LoadConfirmationId == loadConfirmation.Id)).ToList(); // PK - XP for SL by LC ID
        await SendForApprovalAsync(loadConfirmation, salesLines, request);
    }

    private List<string> ValidateLoadConfirmation(LoadConfirmationEntity lc, bool ignoreCurrentStatus)
    {
        List<string> validationErrors = new();

        // check the status
        if (ignoreCurrentStatus == false && lc.Status != LoadConfirmationStatus.Open)
        {
            validationErrors.Add("The Load Confirmation is not eligible for sending.");
        }

        if (lc.IsSignatureRequired && !lc.GetSignatoryEmails().HasText())
        {
            validationErrors.Add("The Load Confirmation requires a signature, no signatories are provided.");
        }

        return validationErrors;
    }

    private DateTimeOffset GetCurrentClientTime()
    {
        // MST / Alberta time zone
        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-7));
    }

    private string CalculateHash(Guid id, LoadConfirmationHashStrategy strategy)
    {
        // use SHA256 for the hash-code generation
        using var sha = SHA256.Create();

        // compute the hash based on the given LC ID
        var hash = sha.ComputeHash(id.ToByteArray());
        var hashString = Convert.ToHexString(hash);

        var length = strategy switch
                     {
                         LoadConfirmationHashStrategy.Version1L16 => 16,
                         LoadConfirmationHashStrategy.Version2L6 => 6,
                         _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
                     };

        // only first 6 chars are used from this hash for generation/validation
        var shortHashString = hashString.Substring(0, length);

        return shortHashString;
    }

    private bool IsHashCorrect(Guid id, string existingHash, LoadConfirmationHashStrategy strategy)
    {
        var computedHash = CalculateHash(id, strategy);

        // compare the existing and computed hashes
        return computedHash == existingHash;
    }

    private async Task<(JObject, List<BlobAttachment>)> GetFieldTicketPayload(LoadConfirmationEntity lc)
    {
        // fetch the corresponding invoice
        var invoice = await _invoiceProvider.GetById(lc.InvoiceId); // PK - TODO: ENTITY or INDEX
        if (invoice == null)
        {
            throw new InvalidOperationException($"Invoice does not exist for this Load Confirmation ({lc.Number}).");
        }

        // fetch the customer info
        var customer = await _accountProvider.GetById(lc.BillingCustomerId);
        if (customer == null)
        {
            throw new InvalidOperationException($"Unable to fetch the customer (BillingCustomerId: {lc.BillingCustomerId}).");
        }

        // fetch the legal entity of the customer
        var legalEntity = await _legalEntityProvider.GetById(customer.LegalEntityId);
        if (legalEntity == null)
        {
            throw new InvalidOperationException($"Unable to fetch the legal entity of the customer (LegalEntityId: {customer.LegalEntityId}).");
        }

        // fetch the billing config
        var billingConfig = await _billingConfigurationProvider.GetById(invoice.BillingConfigurationId);
        if (billingConfig == null)
        {
            throw new InvalidOperationException($"Unable to fetch the billing configuration used in the invoice (BillingConfigurationId: {invoice.BillingConfigurationId}).");
        }

        // fetch the invoice configuration
        var invoiceConfig = await _invoiceConfigurationProvider.GetById(billingConfig.InvoiceConfigurationId);
        if (invoiceConfig == null)
        {
            throw new InvalidOperationException($"Unable to fetch the invoice configuration used in the invoice (InvoiceConfigurationId: {billingConfig.InvoiceConfigurationId}).");
        }

        // fetch the sales lines for this load confirmation
        var salesLines = (await _salesLineProvider.Get(sl => sl.LoadConfirmationId == lc.Id)).ToList(); // PK - XP for SL by LC ID
        if (!salesLines.Any())
        {
            throw new InvalidOperationException($"There are no Sales Lines for this Load Confirmation ({lc.Number}).");
        }

        // NOTE: this model might need an update
        var model = new FieldTicketModel
        {
            Platform = billingConfig.InvoiceExchange.HasText() ? billingConfig.InvoiceExchange : invoiceConfig.InvoiceExchange,
            BillingCustomerId = customer.Id,
            InvoiceAccount = customer.CustomerNumber,
            InvoiceId = lc.Number,
            InvoiceDate = (lc.EndDate ?? DateTimeOffset.Now).DateTime, // no end date = open
            DataAreaId = lc.LegalEntity,
            CurrencyCode = lc.Currency,
            InvoiceTotal = salesLines.Sum(sl => sl.TotalValue),
            TotalLineItems = salesLines.Count,
            LineItems = salesLines.Select(ConvertLines).ToList(),
            BillToDuns = customer.DUNSNumber,
            RemitToDuns = legalEntity.RemitToDuns,
            SummaryIndicator = 1,
            ServicePeriodStartHeader = salesLines.MinBy(sl => sl.TruckTicketDate.UtcDateTime).TruckTicketDate.UtcDateTime,
            EmailDelivery = new(),

            // skip these
            Email = default,
            EdiDate = default,
            Comments = default,
            FirstDescription = default,
            FirstWell = default,
            HeaderCountryCode = default,
            InvoiceTypeCode = default,
            DefaultBuyerDepartment = default,
        };

        // attachments
        var includedDocs = lc.Attachments?.Where(lca => lca.IsIncludedInInvoice == true).ToList();
        var generatedDocs = lc.Attachments?.Where(lca => lca.Autogenerated == true).ToList();
        List<BlobAttachment> attachments = new();

        if (includedDocs.Any())
        {
            attachments = new() { ConvertAttachment(includedDocs.MaxBy(lca => lca.AttachedOn)) };
        }
        else if (generatedDocs.Any())
        {
            attachments = new() { ConvertAttachment(generatedDocs.MaxBy(lca => lca.AttachedOn)) };
        }

        // into JSON
        return (JObject.FromObject(model), attachments);

        FieldTicketLineModel ConvertLines(SalesLineEntity sl, int i)
        {
            return new()
            {
                TotalAmount = sl.TotalValue,
                ItemNumber = sl.ProductNumber,
                Description = sl.ProductName,
                TicketNumber = sl.TruckTicketNumber,
                Quantity = sl.Quantity,
                Rate = sl.Rate,
                UnitsOfMeasure = sl.UnitOfMeasure,
                Edi = ConvertEdi(sl.EdiFieldValues),
                ServicePeriodEndLine = sl.TruckTicketDate.UtcDateTime,
                ServicePeriodStartLine = sl.TruckTicketDate.UtcDateTime,
                LineNumber = i + 1,
                BolNumber = sl.BillOfLading,
                TicketDate = sl.TruckTicketDate.UtcDateTime,
                SourceLocation = sl.SourceLocationFormattedIdentifier,
                SourceLocationType = sl.SourceLocationTypeName,
                InventSite = sl.FacilitySiteId,

                // TODO: tax dependency
                TaxAmount = default,
                Tax = new(),

                // skip these
                DiscountAmount = default,
                DiscountPercent = default,
                PackingGroup = default,
                ApproverRequestor = default,
            };

            FieldTicketLineEdiModel ConvertEdi(List<EDIFieldValueEntity> ediFieldValues)
            {
                var edi = new FieldTicketLineEdiModel();
                foreach (var value in ediFieldValues)
                {
                    TypeExtensions.TrySetNestedPropertyValue(edi, value.EDIFieldName, value.EDIFieldValueContent);
                }

                return edi;
            }
        }

        BlobAttachment ConvertAttachment(LoadConfirmationAttachmentEntity lca)
        {
            return new()
            {
                ContainerName = lca.BlobContainer,
                BlobPath = lca.BlobPath,
                ContentType = lca.ContentType,
                Filename = lca.FileName,
            };
        }
    }

    private async Task AddNote(Guid loadConfirmationId, string text, bool isIntegrations)
    {
        var noteEntity = new NoteEntity
        {
            Id = Guid.NewGuid(),
            Comment = text,
            NotEditable = true,
            ThreadId = $"{Databases.Discriminators.LoadConfirmation}|{loadConfirmationId}",
        };

        // automated notes are not editable
        if (isIntegrations)
        {
            noteEntity.CreatedAt = DateTimeOffset.Now;
            noteEntity.UpdatedAt = DateTimeOffset.Now;
            noteEntity.UpdatedBy = "Integrations";
            noteEntity.CreatedBy = "Integrations";
        }

        await _noteManager.Save(noteEntity);
    }

    private bool IsAllowedExtension(string filenameOrPath)
    {
        // check extensions; includes leading '.'
        var ext = Path.GetExtension(filenameOrPath);
        return AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private async Task SaveAttachments(LoadConfirmationEntity lc, LoadConfirmationApprovalEmail email)
    {
        var notesBuilder = new StringBuilder();
        var now = GetCurrentClientTime();

        // convert into an entity
        foreach (var emailAttachment in email.Attachments)
        {
            if (IsAllowedExtension(emailAttachment.FileName))
            {
                lc.Attachments.Add(new()
                {
                    Id = Guid.NewGuid(),
                    AttachedOn = now,
                    BlobContainer = emailAttachment.BlobContainer,
                    BlobPath = emailAttachment.BlobPath,
                    FileName = emailAttachment.FileName,
                    ContentType = emailAttachment.ContentType,
                    AttachmentOrigin = LoadConfirmationAttachmentOrigin.Integrations,
                    IsIncludedInInvoice = true,
                });
            }
            else
            {
                notesBuilder.AppendLine($"The attachment '{emailAttachment.FileName}' received on {now} cannot be attached due to not allowed file extension.");
            }
        }

        // append notes if there are any
        var notes = notesBuilder.ToString();
        if (notes.HasText())
        {
            await AddNote(lc.Id, notes, false);
        }
    }

    private async Task AppendLoadConfirmationPdf(LoadConfirmationEntity lc, byte[] pdfData, string pdfName, string pdfType)
    {
        // upload & reference is done by ID
        var attachmentId = Guid.NewGuid();

        // store a copy in the blob storage
        var blobPath = $"generated-load-confirmations/{lc.Id}/{attachmentId}";
        await _blobStorage.Upload(_blobStorage.DefaultContainerName, blobPath, new MemoryStream(pdfData));

        // new attachment
        lc.Attachments.Add(new()
        {
            Id = attachmentId,
            AttachedOn = GetCurrentClientTime(),
            BlobContainer = _blobStorage.DefaultContainerName,
            BlobPath = blobPath,
            ContentType = pdfType,
            FileName = pdfName,
            Autogenerated = true,
            AttachmentOrigin = LoadConfirmationAttachmentOrigin.Preview,
        });

        // save the reference to the generated document
        lc.LastGeneratedDocumentId = attachmentId;
    }

    private async Task<(bool isRegenerated, byte[] pdfData, string pdfName, string pdfType)> GenerateDocument(LoadConfirmationEntity lc)
    {
        // fetch the last attached document
        var lastDoc = lc.Attachments.FirstOrDefault(a => a.Id == lc.LastGeneratedDocumentId);

        // TODO: update the document caching strategy and re-enable the Document Regeneration flag
        lc.RequiresDocumentRegeneration = true;

        // generate a document when either it's outdated and requires the regeneration or when there is no document at all
        if (lc.RequiresDocumentRegeneration || lastDoc == null)
        {
            // render the LC PDF
            var pdfData = await _pdfRenderer.RenderLoadConfirmationPdf(lc);
            var pdfName = $"{lc.Number}_{lc.SentCount}.pdf";
            var pdfType = MediaTypeNames.Application.Pdf;

            // save the generated doc at LC level
            await AppendLoadConfirmationPdf(lc, pdfData, pdfName, pdfType);
            lc.RequiresDocumentRegeneration = false;

            // document is re-generated
            return (true, pdfData, pdfName, pdfType);
        }
        else
        {
            // copy data into memory
            using var memoryStream = new MemoryStream();
            await using var docStream = await _blobStorage.Download(lastDoc.BlobContainer, lastDoc.BlobPath);
            await docStream.CopyToAsync(memoryStream);
            await docStream.FlushAsync();

            // init vars
            var pdfData = memoryStream.ToArray();
            var pdfName = lastDoc.FileName;
            var pdfType = lastDoc.ContentType;

            // used latest cached version
            return (false, pdfData, pdfName, pdfType);
        }
    }

    private async Task SendForApprovalAsync(LoadConfirmationEntity lc, List<SalesLineEntity> salesLines, LoadConfirmationAdvancedEmailModel advancedEmailModel)
    {
        // close the LC if it's open for ad-hoc submissions, otherwise this workflow would be kicked off based on certain cadence/schedule
        lc.EndDate ??= GetCurrentClientTime();
        lc.UpdateEffectiveDateRange(salesLines);

        // load the facility for this LC
        var facility = await _facilityProvider.GetById(lc.FacilityId);
        if (facility == null)
        {
            throw new InvalidOperationException($"Facility has not been found for the Load Confirmation '{lc.Number}'. ({lc.Id})");
        }

        // fetch the billing config
        var billingConfig = await _billingConfigurationProvider.GetById(lc.BillingConfigurationId);
        if (billingConfig == null)
        {
            throw new InvalidOperationException($"Billing configuration has not been found for the Load Confirmation '{lc.Number}'. ({lc.Id})");
        }

        // the recipients are signatories that are attached to the billing config that used to generate this LC
        var signatoryEmails = billingConfig.GetSignatoryEmails();
        var ccEmails = billingConfig.GetEmailContacts();

        // generate a PDF document according to rules/requirements
        var (_, pdfData, pdfName, pdfType) = await GenerateDocument(lc);

        // send the email; pass all data needed for the email template
        var emailModel = new LoadConfirmationApprovalEmailModel
        {
            LoadConfirmation = lc,
            LoadConfirmationNumber = lc.Number,
            LoadConfirmationHash = CalculateHash(lc.Id, LoadConfirmationHashStrategy.Version2L6),
            LoadConfirmationStream = new MemoryStream(pdfData),
            ContentType = pdfType,
            Filename = pdfName,
        };

        await SendEmail(EmailTemplateEventNames.RequestLoadConfirmationApproval, signatoryEmails, ccEmails, emailModel, null, facility.SiteId, lc.BillingCustomerId, null, advancedEmailModel);

        // update the LC status once the email is sent
        lc.Status = LoadConfirmationStatus.PendingSignature;
        lc.SentCount++;
        lc.LastApprovalEmailSentOn = GetCurrentClientTime();

        // add a note with details of the email
        {
            var notesBuilder = new StringBuilder();
            notesBuilder.AppendLine("Load Confirmation has been sent with the following details:");
            if (_userContextAccessor?.UserContext?.DisplayName.HasText() == true)
            {
                notesBuilder.AppendLine($"Sender: {_userContextAccessor?.UserContext?.DisplayName}");
            }

            if (advancedEmailModel?.IsCustomeEmail == true)
            {
                notesBuilder.AppendLine($"Signatory: {(advancedEmailModel.To.HasText() ? advancedEmailModel.To : string.Empty)}");

                notesBuilder.AppendLine($"Cc Emails: {(advancedEmailModel.Cc.HasText() ? advancedEmailModel.Cc : string.Empty)}");

                notesBuilder.AppendLine($"Bcc Emails: {(advancedEmailModel.Bcc.HasText() ? advancedEmailModel.Bcc : string.Empty)}");
            }
            else if (signatoryEmails.HasText())
            {
                notesBuilder.AppendLine($"Signatory: {signatoryEmails}");
            }

            notesBuilder.AppendLine($"Date: {GetCurrentClientTime()}");

            notesBuilder.AppendLine($"Send#: {lc.SentCount}");

            await AddNote(lc.Id, notesBuilder.ToString(), false);
        }
    }

    private async Task SendEmail(string templateKey,
                                 string recipients,
                                 string ccRecipients,
                                 LoadConfirmationApprovalEmailModel loadConfirmationApprovalEmailModel,
                                 LoadConfirmationTamperedEmailModel loadConfirmationTamperedEmailModel,
                                 string facilitySiteId,
                                 Guid accountId,
                                 List<AdHocAttachment> blobAttachments,
                                 LoadConfirmationAdvancedEmailModel loadConfirmationAdvancedEmailModel)
    {
        await _emailTemplateSender.Dispatch(new()
        {
            TemplateKey = templateKey,
            Recipients = recipients,
            CcRecipients = ccRecipients,
            BccRecipients = string.Empty,
            AdHocNote = string.Empty,
            AdHocAttachments = blobAttachments ?? new(),
            ContextBag = new()
            {
                [nameof(LoadConfirmationApprovalEmailModel)] = loadConfirmationApprovalEmailModel,
                [nameof(LoadConfirmationTamperedEmailModel)] = loadConfirmationTamperedEmailModel,
                [nameof(LoadConfirmationAdvancedEmailModel)] = loadConfirmationAdvancedEmailModel,
                [nameof(LoadConfirmationEntity.SiteId)] = facilitySiteId,
                [nameof(LoadConfirmationEntity.BillingCustomerId)] = accountId,
            },
        });
    }

    private Task SetWaitingForInvoiceAsync(LoadConfirmationEntity lc)
    {
        // update the status to 'waiting for invoice'
        lc.Status = LoadConfirmationStatus.WaitingForInvoice;
        return Task.CompletedTask;
    }

    private async Task DeliverFieldTicketAsync(LoadConfirmationEntity lc)
    {
        lc.EndDate ??= GetCurrentClientTime();

        // ensure the latest document exists for a preview if there are no included attachments
        if (lc.LastGeneratedDocumentId == null && !lc.Attachments.Where(lca => lca.IsIncludedInInvoice == true).Any())
        {
            await GenerateDocument(lc);
        }

        // prep SB request
        var (payload, blobs) = await GetFieldTicketPayload(lc);
        var request = new DeliveryRequest
        {
            EnterpriseId = lc.Id,
            CorrelationId = Guid.NewGuid().ToString(),
            Source = "TT",
            Operation = "Send",
            MessageType = MessageType.FieldTicketRequest.ToString(),
            MessageDate = DateTime.UtcNow,
            Payload = payload,
            Blobs = blobs,
        };

        // send to SB
        await _invoiceDeliveryServiceBus.EnqueueRequest(request);

        // update the status upon submission to the Invoice Gateway
        lc.Status = LoadConfirmationStatus.SubmittedToGateway;

        // leave a note
        await AddNote(lc.Id, "Field Ticket has been queued for submission.", false);
    }

    private bool? ValidateApprovalEmailAsync(LoadConfirmationEntity lc, string parsedHash, LoadConfirmationHashStrategy strategy)
    {
        if (!parsedHash.HasText())
        {
            // inconclusive, the subject line doesn't have the correct tracking code or no hash
            return null;
        }

        return IsHashCorrect(lc.Id, parsedHash, strategy);
    }

    private async Task ForwardTamperedEmailToFacilityAsync(LoadConfirmationEntity lc, LoadConfirmationApprovalEmail ae)
    {
        // load facilities
        var facility = await _facilityProvider.GetById(lc.FacilityId);
        if (facility == null)
        {
            // no facility = no admin email (recipient)
            return;
        }

        // subject with pattern: "{ Unidentifiable Signatory Email } Original subject: "
        var emailModel = new LoadConfirmationTamperedEmailModel
        {
            LoadConfirmationNumber = lc.Number,
            OriginalFrom = ae.From,
            OriginalSubject = ae.Subject,
        };

        // re-attach the attachments to the new email
        var attachments = ae.Attachments.Select(a => new AdHocAttachment
        {
            ContainerName = a.BlobContainer,
            BlobPath = a.BlobPath,
            ContentType = a.ContentType,
            FileName = a.FileName,
        }).ToList();

        // send the email
        await SendEmail(EmailTemplateEventNames.LoadConfirmationApprovalTampered, facility.AdminEmail, string.Empty, null, emailModel, facility.SiteId, lc.BillingCustomerId, attachments, null);
    }

    private async Task SaveAttachmentsAndAddNoteAsync(LoadConfirmationEntity lc, LoadConfirmationApprovalEmail em)
    {
        // add a note
        var n = Environment.NewLine;
        var clientDate = GetCurrentClientTime();
        var notes = $"{clientDate} {em.From} {em.Subject} {n}{n} {em.Body}";
        await AddNote(lc.Id, notes, false);

        // copy attachment links
        await SaveAttachments(lc, em);
    }

    private async Task RejectAndAddNoteAsync(LoadConfirmationEntity lc, LoadConfirmationApprovalEmail em)
    {
        // set the status to rejected
        lc.Status = LoadConfirmationStatus.Rejected;

        // add a note if there are encrypted attachments
        if (em.HasEncryptedAttachments)
        {
            var notesBuilder = new StringBuilder();
            notesBuilder.AppendLine("The following attachment(s) are encrypted PDFs and require manual intervention.");
            foreach (var attachment in em.Attachments.Where(a => a.IsEncrypted))
            {
                notesBuilder.AppendLine(attachment.FileName);
            }

            // save the note
            await AddNote(lc.Id, notesBuilder.ToString(), false);
        }
    }

    private Task SetWaitingSignatureValidationAsync(LoadConfirmationEntity lc)
    {
        lc.Status = LoadConfirmationStatus.WaitingSignatureValidation;
        return Task.CompletedTask;
    }

    private async Task ApproveSignature(LoadConfirmationEntity loadConfirmation, string userComment)
    {
        // signature verified manually
        loadConfirmation.Status = LoadConfirmationStatus.SignatureVerified;
        loadConfirmation.IsSignatureRequired = false;
        await _loadConfirmationManager.Save(loadConfirmation);

        // leave a note
        await AddNote(loadConfirmation.Id, "Load Confirmation approval has been granted.", false);

        // kick off the workflow after the signature stage
        await StartFromBeginning(loadConfirmation.Key, string.Empty, true);
    }

    private async Task RejectSignature(LoadConfirmationEntity loadConfirmation, string userComment)
    {
        // change state
        loadConfirmation.Status = LoadConfirmationStatus.Rejected;
        await _loadConfirmationManager.Save(loadConfirmation);

        // leave a note
        await AddNote(loadConfirmation.Id, userComment, false);
    }

    private async Task MarkLoadConfirmationReady(LoadConfirmationEntity loadConfirmation, string userComment)
    {
        // if the LC is Open, set the end date to prevent addition of the new sales lines
        if (loadConfirmation.Status == LoadConfirmationStatus.Open)
        {
            loadConfirmation.EndDate = DateTimeOffset.Now.ToAlbertaOffset();
        }

        // marking it Ready
        loadConfirmation.Status = LoadConfirmationStatus.WaitingForInvoice;
        await _loadConfirmationManager.Save(loadConfirmation);

        // leave a note
        await AddNote(loadConfirmation.Id, "Load Confirmation has been marked Ready.", false);
    }

    private async Task VoidLoadConfirmation(LoadConfirmationEntity loadConfirmation, string userComment)
    {
        // remove all sales lines from the voided load confirmation 
        var salesLines = (await _salesLineProvider.Get(sl => sl.LoadConfirmationId == loadConfirmation.Id)).ToList(); // PK - XP for SL by LC ID
        if (salesLines.Any(sl => sl.Status != SalesLineStatus.Void))
        {
            // NOTE: in the new workflow, voiding of the LC is allowed only when there are no sales lines in it
            return;
        }

        // change state
        loadConfirmation.Status = LoadConfirmationStatus.Void;
        await _loadConfirmationManager.Save(loadConfirmation);

        foreach (var salesLine in salesLines)
        {
            var newSalesLine = salesLine.CloneAsNew();

            // keep the historical ID permanently only when it's not initialized
            if (salesLine.HistoricalInvoiceId.HasValue == false && salesLine.InvoiceId.HasValue)
            {
                salesLine.HistoricalInvoiceId = salesLine.InvoiceId;
            }

            salesLine.InvoiceId = null;
            salesLine.ProformaInvoiceNumber = null;
            salesLine.LoadConfirmationId = null;
            salesLine.LoadConfirmationNumber = null;
            salesLine.Status = SalesLineStatus.Void;
            salesLine.AwaitingRemovalAcknowledgment = true;

            await _salesLineProvider.Update(salesLine, true);
            await _salesLineProvider.Insert(newSalesLine, true);
        }

        // update truck tickets for the given sales lines
        var truckTicketIds = salesLines.Select(sl => sl.TruckTicketId).ToHashSet();
        var truckTickets = await _truckTicketProvider.Get(tt => truckTicketIds.Contains(tt.Id)); // PK - TODO: ENTITY or INDEX
        foreach (var truckTicket in truckTickets)
        {
            truckTicket.Status = TruckTicketStatus.Open;
            await _truckTicketProvider.Update(truckTicket, true);
        }

        // leave a note
        await AddNote(loadConfirmation.Id, $"Load Confirmation has been Voided with the following reason:{Environment.NewLine}{userComment}", false);

        await _loadConfirmationManager.SaveDeferred();
    }
}
