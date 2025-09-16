using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.LoadSummaryReport)]
public class LoadSummaryEmailTemplateProcessor : EmailTemplateProcessorBase
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    public LoadSummaryEmailTemplateProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                             IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                             IProvider<Guid, FacilityEntity> facilityProvider,
                                             IProvider<Guid, AccountEntity> accountProvider,
                                             IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider)
        : base(emailTemplateRenderer, emailTemplateProvider, emailTemplateEventProvider)
    {
        _facilityProvider = facilityProvider;
        _accountProvider = accountProvider;
    }

    public override async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
    {
        var materialApproval = GetMaterialApproval(context);

        context.CurrentFacility = await _facilityProvider.GetById(materialApproval.FacilityId);
        context.CurrentAccount = await _accountProvider.GetById(materialApproval.BillingCustomerId);

        return await ResolveEmailTemplate(EmailTemplateEventNames.LoadSummaryReport, context.CurrentFacility.SiteId, context.CurrentAccount.Id);
    }

    protected override async ValueTask AddAttachments(EmailTemplateProcessingContext context)
    {
        if (!context.TemplateDeliveryRequest.ContextBag.ContainsKey("PDF"))
        {
            return;
        }

        var pdfJson = context.TemplateDeliveryRequest.ContextBag["PDF"] as string;
        var buffer = pdfJson.FromJson<byte[]>();

        // add an email attachment
        var stream = new MemoryStream(buffer);

        var name = $"LoadSummaryReport-{DateTime.UtcNow:MMM-dd-yyyy}.pdf";
        context.MailMessage.Attachments.Add(new(stream, name, MediaTypeNames.Application.Pdf));

        // call base
        await base.AddAttachments(context);
    }

    protected override async ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        var materialApproval = GetMaterialApproval(context);

        var facility = await _facilityProvider.GetById(materialApproval.FacilityId);

        var data = new DataObject
        {
            MaterialApproval = materialApproval,
            Facility = facility,
            UserContext = context.TemplateDeliveryRequest.GetValueOrDefaultFromContextBag<UserContext>(nameof(UserContext), new()),
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

        public FacilityEntity Facility { get; set; }

        public UserContext UserContext { get; set; }
    }
}
