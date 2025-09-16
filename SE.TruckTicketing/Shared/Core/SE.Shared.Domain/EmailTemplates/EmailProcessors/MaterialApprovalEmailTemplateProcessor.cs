using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.MaterialApprovalConfirmation)]
public class MaterialApprovalEmailTemplateProcessor : EmailTemplateProcessorBase
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    private readonly IEmailTemplateAttachmentBlobStorage _adHocAttachmentBlobStorage;

    public MaterialApprovalEmailTemplateProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                                  IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                                  IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider,
                                                  IProvider<Guid, FacilityEntity> facilityProvider,
                                                  IProvider<Guid, AccountEntity> accountProvider,
                                                  IProvider<Guid, SourceLocationEntity> sourceLocationProvider,
                                                  IUserContextAccessor userContextAccessor,
                                                  IEmailTemplateAttachmentBlobStorage adHocAttachmentBlobStorage)
        : base(emailTemplateRenderer,
               emailTemplateProvider,
               emailTemplateEventProvider)
    {
        _facilityProvider = facilityProvider;
        _accountProvider = accountProvider;
        _sourceLocationProvider = sourceLocationProvider;
        _userContextAccessor = userContextAccessor;
        _adHocAttachmentBlobStorage = adHocAttachmentBlobStorage;
    }

    public override async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
    {
        var materialApproval = GetMaterialApproval(context);

        context.CurrentFacility = await _facilityProvider.GetById(materialApproval.FacilityId);
        context.CurrentAccount = await _accountProvider.GetById(materialApproval.BillingCustomerId);

        return await ResolveEmailTemplate(EmailTemplateEventNames.MaterialApprovalConfirmation, context.CurrentFacility.SiteId, context.CurrentAccount.Id);
    }

    protected override async ValueTask AddAttachments(EmailTemplateProcessingContext context)
    {
        if (!context.TemplateDeliveryRequest.ContextBag.ContainsKey("PDF"))
        {
            return;
        }

        var pdfJson = context.TemplateDeliveryRequest.ContextBag["PDF"] as string;
        var buffer = pdfJson.FromJson<byte[]>();

        // add email attachment
        var stream = new MemoryStream(buffer);

        var name = $"MaterialApproval-{DateTime.UtcNow:MMM-dd-yyyy}.pdf";
        context.MailMessage.Attachments.Add(new(stream, name, MediaTypeNames.Application.Pdf));

        // attach adhoc attachments
        if (context.TemplateDeliveryRequest.AdHocAttachments != null)
        {
            foreach (AdHocAttachment attachment in context.TemplateDeliveryRequest.AdHocAttachments)
            {
                var attStream = await _adHocAttachmentBlobStorage.Download(attachment.ContainerName ?? _adHocAttachmentBlobStorage.DefaultContainerName, attachment.BlobPath);
                context.MailMessage.Attachments.Add(new(await attStream.Memorize(), attachment.FileName, attachment.ContentType));
            }
        }

        // call base
        await base.AddAttachments(context);
    }

    protected override async ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        var materialApproval = GetMaterialApproval(context);

        var facility = await _facilityProvider.GetById(materialApproval.FacilityId);
        var sourceLocation = await _sourceLocationProvider.GetById(materialApproval.SourceLocationId);
        var userContext = _userContextAccessor.UserContext;
        var serviceTypeClass = string.Empty;

        if (context.TemplateDeliveryRequest.ContextBag.ContainsKey("Class"))
        {
            serviceTypeClass = (string)context.TemplateDeliveryRequest.ContextBag["Class"];
        }

        var data = new DataObject
        {
            MaterialApproval = materialApproval,
            SourceLocation = sourceLocation,
            Facility = facility,
            UserContext = userContext,
            Class = serviceTypeClass,
            AdHocNote = context.TemplateDeliveryRequest.AdHocNote,
        };

        return data;
    }

    private MaterialApproval GetMaterialApproval(EmailTemplateProcessingContext context)
    {
        var materialApprovalJson = context.TemplateDeliveryRequest.ContextBag[nameof(MaterialApproval)] as string;
        var materialApproval = materialApprovalJson.FromJson<MaterialApproval>();
        return materialApproval;
    }

    public class DataObject
    {
        public MaterialApproval MaterialApproval { get; set; }

        public SourceLocationEntity SourceLocation { get; set; }

        public FacilityEntity Facility { get; set; }

        public UserContext UserContext { get; set; }

        public string Class { get; set; }

        public string AdHocNote { get; set; }
    }
}
