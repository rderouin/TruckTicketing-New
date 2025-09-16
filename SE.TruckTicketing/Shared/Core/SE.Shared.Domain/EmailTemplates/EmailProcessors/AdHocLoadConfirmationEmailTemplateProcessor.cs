using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.AdHocLoadConfirmation)]
public class AdHocLoadConfirmationEmailTemplateProcessor : EmailTemplateProcessorBase
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly ILoadConfirmationPdfRenderer _loadConfirmationPdfRenderer;

    private readonly IUserContextAccessor _userContextAccessor;

    public AdHocLoadConfirmationEmailTemplateProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                                       IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                                       IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider,
                                                       IProvider<Guid, FacilityEntity> facilityProvider,
                                                       IProvider<Guid, AccountEntity> accountProvider,
                                                       ILoadConfirmationPdfRenderer loadConfirmationPdfRenderer,
                                                       IUserContextAccessor userContextAccessor)
        : base(emailTemplateRenderer,
               emailTemplateProvider,
               emailTemplateEventProvider)
    {
        _facilityProvider = facilityProvider;
        _accountProvider = accountProvider;
        _loadConfirmationPdfRenderer = loadConfirmationPdfRenderer;
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

        return await ResolveEmailTemplate(EmailTemplateEventNames.AdHocLoadConfirmation, facilityId, accountId);
    }

    protected override async ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        var renderModel = new EmailRenderModel
        {
            Request = context.TemplateDeliveryRequest,
        };

        var facilityId = context.TemplateDeliveryRequest.ContextBag[nameof(LoadConfirmationEntity.SiteId)] as string;
        if (facilityId.HasText())
        {
            renderModel.Facility = (await _facilityProvider.Get(f => f.SiteId == facilityId)).FirstOrDefault();
        }

        renderModel.UserContext = _userContextAccessor.UserContext;

        return renderModel;
    }

    protected override async ValueTask AddAttachments(EmailTemplateProcessingContext context)
    {
        // context bag should have sales lines to render
        var json = (string)context.TemplateDeliveryRequest.ContextBag[nameof(SalesLine)];
        var attachmentIndicatorTypeString = (string)context.TemplateDeliveryRequest.ContextBag[nameof(AttachmentIndicatorType)];

        // parse
        var salesLineIds = json.FromJson<List<CompositeKey<Guid>>>();
        var attachmentIndicatorType = Enum.Parse<AttachmentIndicatorType>(attachmentIndicatorTypeString);

        // generate the LC for the given SLs
        await AddLoadConfirmation(context, salesLineIds, attachmentIndicatorType);

        // call base
        await base.AddAttachments(context);
    }

    private async Task AddLoadConfirmation(EmailTemplateProcessingContext context, List<CompositeKey<Guid>> salesLineKeys, AttachmentIndicatorType attachmentIndicatorType)
    {
        // render the LC PDF
        var pdfDoc = await _loadConfirmationPdfRenderer.RenderAdHocLoadConfirmationPdf(new()
        {
            SalesLineKeys = salesLineKeys,
            AttachmentType = attachmentIndicatorType,
        });

        // add an email attachment
        var stream = new MemoryStream(pdfDoc);
        var name = $"LoadConfirmation-AdHoc-{DateTime.UtcNow:MMM-dd-yyyy}.pdf";
        context.MailMessage.Attachments.Add(new(stream, name, MediaTypeNames.Application.Pdf));
    }

    public class EmailRenderModel
    {
        public EmailTemplateDeliveryRequest Request { get; set; }

        public FacilityEntity Facility { get; set; }

        public UserContext UserContext { get; set; }
    }
}
