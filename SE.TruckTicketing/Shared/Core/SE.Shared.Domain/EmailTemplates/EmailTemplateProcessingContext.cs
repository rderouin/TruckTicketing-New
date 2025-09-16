using System;
using System.Net.Mail;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;

namespace SE.Shared.Domain.EmailTemplates;

public class EmailTemplateProcessingContext
{
    public EmailTemplateProcessingContext(EmailTemplateDeliveryRequest templateDeliveryRequest)
    {
        TemplateDeliveryRequest = templateDeliveryRequest;
        MailMessage = new();
    }

    public EmailTemplateDeliveryRequest TemplateDeliveryRequest { get; }

    public EmailTemplateEntity EmailTemplate { get; set; }

    public MailMessage MailMessage { get; }
    
    public FacilityEntity CurrentFacility { get; set; }
    
    public AccountEntity CurrentAccount { get; set; }

    public bool IsMessageSent { get; set; }

    public Exception Exception { get; set; }
}
