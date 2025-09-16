using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.CreditApplicationRequestDetails)]
public class CreditApplicationEmailTemplateProcessor : EmailTemplateProcessorBase
{
    public CreditApplicationEmailTemplateProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                                   IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                                   IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider)
        : base(emailTemplateRenderer,
               emailTemplateProvider,
               emailTemplateEventProvider)
    {
    }

    public override async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
    {
        // NOTE: this email is sent upon account creation hence cannot have facility/customer customizations
        return await ResolveEmailTemplate(EmailTemplateEventNames.CreditApplicationRequestDetails, null, null);
    }

    protected override ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        var request = context.TemplateDeliveryRequest;

        var model = new EmailRenderModel
        {
            Account = request.GetValueOrDefaultFromContextBag<AccountEntity>(nameof(AccountEntity), new()),
            UserContext = request.GetValueOrDefaultFromContextBag<UserContext>(nameof(UserContext), new()),
            Facility = null, // NOTE: what's the link to a facility from a newly created account?
        };

        return ValueTask.FromResult<object>(model);
    }

    protected override async ValueTask AddAttachments(EmailTemplateProcessingContext context)
    {
        var account = context.TemplateDeliveryRequest.GetValueOrDefaultFromContextBag<AccountEntity>(nameof(AccountEntity), new());

        var name = string.Empty;
        if (account.LegalEntity.ToLower() == "sesc")
        {
            name = "SESC-Credit-Application.pdf";
        }
        else if (account.LegalEntity.ToLower() == "sesu")
        {
            name = "SESU-Credit-Application.pdf";
        }

        if (!name.HasText())
        {
            return;
        }

        try
        {
            // add an email attachment
            var stream = File.OpenRead($"EmailProcessorAttachments/{name}");
            context.MailMessage.Attachments.Add(new(stream, name, MediaTypeNames.Application.Pdf));
        }
        catch { }

        // call base
        await base.AddAttachments(context);
    }

    public class EmailRenderModel
    {
        public AccountEntity Account { get; set; }

        public FacilityEntity Facility { get; set; }

        public UserContext UserContext { get; set; }
    }
}
