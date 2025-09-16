using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.CreditCardPaymentRequest)]
public class CreditCardPaymentEmailTemplateProcessor : EmailTemplateProcessorBase
{
    public CreditCardPaymentEmailTemplateProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                                   IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                                   IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider)
        : base(emailTemplateRenderer,
               emailTemplateProvider,
               emailTemplateEventProvider)
    {
    }

    public override async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
    {
        if (context.TemplateDeliveryRequest.ContextBag.TryGetValue(nameof(FacilityEntity), out var modelObj) && modelObj is FacilityEntity model)
        {
            context.CurrentFacility = model;
        }

        return await ResolveEmailTemplate(EmailTemplateEventNames.CreditCardPaymentRequest, context.CurrentFacility?.SiteId, null);
    }

    protected override ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        EmailRenderModel renderModel;

        if (context.TemplateDeliveryRequest.ContextBag.TryGetValue(nameof(FacilityEntity), out var modelObj) && modelObj is FacilityEntity model)
        {
            renderModel = new()
            {
                Facility = model,
            };
        }
        else
        {
            renderModel = new();
        }

        return ValueTask.FromResult<object>(renderModel);
    }

    public class EmailRenderModel
    {
        public FacilityEntity Facility { get; set; }
    }
}
