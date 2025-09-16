using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.EmailTemplates.EmailProcessors.LoadConfirmation;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.LoadConfirmationApprovalTampered)]
public class LoadConfirmationTamperedTemplateProcessor : EmailTemplateProcessorBase
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly ITruckTicketUploadBlobStorage _truckTicketBlobStorage;

    private readonly IUserContextAccessor _userContextAccessor;

    public LoadConfirmationTamperedTemplateProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                                     IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                                     IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider,
                                                     ITruckTicketUploadBlobStorage truckTicketBlobStorage,
                                                     IProvider<Guid, FacilityEntity> facilityProvider,
                                                     IProvider<Guid, AccountEntity> accountProvider,
                                                     IUserContextAccessor userContextAccessor)
        : base(emailTemplateRenderer,
               emailTemplateProvider,
               emailTemplateEventProvider)
    {
        _truckTicketBlobStorage = truckTicketBlobStorage;
        _facilityProvider = facilityProvider;
        _accountProvider = accountProvider;
        _userContextAccessor = userContextAccessor;
    }

    public override async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
    {
        var facilityId = context.TemplateDeliveryRequest.ContextBag[nameof(LoadConfirmationEntity.SiteId)] as string;
        var accountId = context.TemplateDeliveryRequest.ContextBag[nameof(LoadConfirmationEntity.BillingCustomerId)] as Guid?;

        if (facilityId.HasText())
        {
            context.CurrentFacility = (await _facilityProvider.Get(f => f.SiteId == facilityId)).FirstOrDefault();
        }

        if (accountId.HasValue)
        {
            context.CurrentAccount = await _accountProvider.GetById(accountId);
        }

        return await ResolveEmailTemplate(EmailTemplateEventNames.LoadConfirmationApprovalTampered, facilityId, accountId);
    }

    protected override async ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        EmailRenderModel renderModel = new();

        if (context.TemplateDeliveryRequest.ContextBag.TryGetValue(nameof(LoadConfirmationTamperedEmailModel), out var modelObj) && modelObj is LoadConfirmationTamperedEmailModel model)
        {
            renderModel.LoadConfirmationNumber = model.LoadConfirmationNumber;
            renderModel.OriginalFrom = model.OriginalFrom;
            renderModel.OriginalSubject = model.OriginalSubject;
        }

        var facilityId = context.TemplateDeliveryRequest.ContextBag[nameof(LoadConfirmationEntity.SiteId)] as string;
        if (facilityId.HasText())
        {
            renderModel.Facility = (await _facilityProvider.Get(f => f.SiteId == facilityId)).FirstOrDefault();
        }

        renderModel.UserContext = _userContextAccessor.UserContext;
        renderModel.AdHocNote = context.TemplateDeliveryRequest.AdHocNote;

        return renderModel;
    }

    protected override async ValueTask AddAttachments(EmailTemplateProcessingContext context)
    {
        await AddLcAttachments(context);
        await base.AddAttachments(context);
    }

    private async Task AddLcAttachments(EmailTemplateProcessingContext context)
    {
        // extra attachments
        foreach (var attachment in context.TemplateDeliveryRequest.AdHocAttachments)
        {
            var attStream = await _truckTicketBlobStorage.Download(attachment.ContainerName ?? _truckTicketBlobStorage.DefaultContainerName, attachment.BlobPath);
            context.MailMessage.Attachments.Add(new(await attStream.Memorize(), attachment.FileName, attachment.ContentType));
        }
    }

    public class EmailRenderModel
    {
        public string AdHocNote { get; set; }

        public string LoadConfirmationNumber { get; set; }

        public string OriginalFrom { get; set; }

        public string OriginalSubject { get; set; }

        public FacilityEntity Facility { get; set; }

        public UserContext UserContext { get; set; }
    }
}
