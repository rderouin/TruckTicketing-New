using System;
using System.IO;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;

using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.Shared.Domain.EmailTemplates.EmailProcessors;

[EmailTemplateProcessorFor(EmailTemplateEventNames.InvoicePaymentRequest)]
public class InvoicePaymentRequestTemplateProcessor : EmailTemplateProcessorBase
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    public InvoicePaymentRequestTemplateProcessor(IEmailTemplateRenderer emailTemplateRenderer,
                                                  IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                                  IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider,
                                                  IProvider<Guid, FacilityEntity> facilityProvider,
                                                  IProvider<Guid, AccountEntity> accountProvider,
                                                  IUserContextAccessor userContextAccessor)
        : base(emailTemplateRenderer,
               emailTemplateProvider,
               emailTemplateEventProvider)
    {
        _facilityProvider = facilityProvider;
        _accountProvider = accountProvider;
        _userContextAccessor = userContextAccessor;
    }

    public override async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context)
    {
        var invoice = GetInvoice(context);

        context.CurrentFacility = await _facilityProvider.GetById(invoice.FacilityId);
        context.CurrentAccount = await _accountProvider.GetById(invoice.CustomerId);

        return await ResolveEmailTemplate(EmailTemplateEventNames.InvoicePaymentRequest, context.CurrentFacility.SiteId, context.CurrentAccount.Id);
    }

    protected override ValueTask AddAttachments(EmailTemplateProcessingContext context)
    {
        if (context.TemplateDeliveryRequest.ContextBag.TryGetValue("PDF", out var dataObj) && dataObj is string jsonData &&
            context.TemplateDeliveryRequest.ContextBag.TryGetValue(nameof(InvoiceAttachment), out var attObj) && attObj is string attJson)
        {
            var attachment = attJson.FromJson<InvoiceAttachment>();
            var stream = new MemoryStream(jsonData.FromJson<byte[]>());
            context.MailMessage.Attachments.Add(new(stream, attachment.FileName, attachment.ContentType));
        }

        return base.AddAttachments(context);
    }

    public override ValueTask BeforeSend(EmailTemplateProcessingContext context)
    {
        var invoiceEmailUpdateRequest = GetInvoiceEmailUpdateRequest(context);

        if (invoiceEmailUpdateRequest.IsCustomeEmail)
        {
            context.TemplateDeliveryRequest.Recipients = invoiceEmailUpdateRequest.To;
            context.TemplateDeliveryRequest.CcRecipients = invoiceEmailUpdateRequest.Cc;
            context.TemplateDeliveryRequest.BccRecipients = invoiceEmailUpdateRequest.Bcc;
            context.TemplateDeliveryRequest.AdHocNote = invoiceEmailUpdateRequest.AdHocNote;
        }

        return base.BeforeSend(context);
    }

    protected override async ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context)
    {
        var invoice = GetInvoice(context);
        var facility = await _facilityProvider.GetById(invoice.FacilityId);

        var data = new DataObject
        {
            CustomerName = invoice.CustomerName,
            InvoiceNumber = invoice.GlInvoiceNumber,
            Facility = facility,
            UserContext = _userContextAccessor.UserContext,
            AdHocNote = context.TemplateDeliveryRequest.AdHocNote,
        };

        return data;
    }

    private Invoice GetInvoice(EmailTemplateProcessingContext context)
    {
        var invoiceJson = context.TemplateDeliveryRequest.ContextBag[nameof(Invoice)] as string;
        var invoice = invoiceJson.FromJson<Invoice>();
        return invoice;
    }

    private InvoiceAdvancedEmailRequest GetInvoiceEmailUpdateRequest(EmailTemplateProcessingContext context)
    {
        var invoiceEmailUpdateRequest = new InvoiceAdvancedEmailRequest();
        if (context.TemplateDeliveryRequest.ContextBag.TryGetValue(nameof(InvoiceAdvancedEmailRequest), out var modelObj) && modelObj is InvoiceAdvancedEmailRequest model)
        {
            invoiceEmailUpdateRequest = model;
        }

        return invoiceEmailUpdateRequest;
    }

    public class DataObject
    {
        public string CustomerName { get; set; }

        public string InvoiceNumber { get; set; }

        public FacilityEntity Facility { get; set; }

        public UserContext UserContext { get; set; }

        public string AdHocNote { get; set; }
    }
}
