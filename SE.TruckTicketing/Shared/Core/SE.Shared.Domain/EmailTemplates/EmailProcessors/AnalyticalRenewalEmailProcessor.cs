using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.AnalyticalRenewal)]
public class AnalyticalRenewalEmailProcessor : EmailTemplateProcessorBase
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    public AnalyticalRenewalEmailProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                           IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                           IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider,
                                           IProvider<Guid, FacilityEntity> facilityProvider,
                                           IProvider<Guid, AccountEntity> accountProvider,
                                           IProvider<Guid, SourceLocationEntity> sourceLocationProvider)
        : base(emailTemplateRenderer, emailTemplateProvider, emailTemplateEventProvider)
    {
        _facilityProvider = facilityProvider;
        _accountProvider = accountProvider;
        _sourceLocationProvider = sourceLocationProvider;
    }

    public override async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
    {
        var materialApproval = GetMaterialApproval(context);

        context.CurrentFacility = await _facilityProvider.GetById(materialApproval.FacilityId);
        context.CurrentAccount = await _accountProvider.GetById(materialApproval.BillingCustomerId);

        return await ResolveEmailTemplate(EmailTemplateEventNames.AnalyticalRenewal, context.CurrentFacility.SiteId, context.CurrentAccount.Id);
    }

    protected override async ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        var materialApproval = GetMaterialApproval(context);

        var facility = await _facilityProvider.GetById(materialApproval.FacilityId);
        var sourceLocation = await _sourceLocationProvider.GetById(materialApproval.SourceLocationId);

        var data = new DataObject
        {
            MaterialApproval = materialApproval,
            SourceLocation = sourceLocation,
            Facility = facility,
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
    }
}
