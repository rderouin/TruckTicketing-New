using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;

namespace SE.Shared.Domain.EmailTemplates;

public abstract class EmailTemplateProcessorBase : IEmailTemplateProcessor
{
    private readonly IProvider<Guid, EmailTemplateEventEntity> _emailTemplateEventProvider;

    private readonly IProvider<Guid, EmailTemplateEntity> _emailTemplateProvider;

    private readonly IEmailTemplateRenderer _emailTemplateRenderer;

    protected EmailTemplateProcessorBase(IEmailTemplateRenderer emailTemplateRenderer,
                                         IProvider<Guid, EmailTemplateEntity> emailTemplateProvider,
                                         IProvider<Guid, EmailTemplateEventEntity> emailTemplateEventProvider)
    {
        _emailTemplateRenderer = emailTemplateRenderer;
        _emailTemplateProvider = emailTemplateProvider;
        _emailTemplateEventProvider = emailTemplateEventProvider;
    }

    public virtual async ValueTask BeforeSend(EmailTemplateProcessingContext context)
    {
        var message = context.MailMessage;

        SetFromMailAddress(context);
        SetToMailAddresses(context);
        SetCcMailAddresses(context);
        SetBccMailAddresses(context);
        SetReplyToMailAddresses(context);

        message.Subject = await RenderSubject(context);
        message.Body = await RenderBody(context);

        await AddAttachments(context);
    }

    public virtual ValueTask AfterSend(EmailTemplateProcessingContext context)
    {
        return ValueTask.CompletedTask;
    }

    public abstract ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context);

    protected async ValueTask<EmailTemplateEntity> ResolveEmailTemplate(string emailTemplateKey, string facilitySiteId, Guid? accountId)
    {
        // ======================================== STEP 1. Fetch Event-Specific Templates ========================================

        // fetch all templates for the event
        var allEventTemplates = (await _emailTemplateProvider.Get(template => template.EventName == emailTemplateKey && template.IsActive == true)).ToList();

        // ======================================== STEP 2. Account for the Facility-Specific Templates ===========================

        // narrow down the list of templates if the facility context is provided
        var facilityFilteredTemplates = new List<EmailTemplateEntity>();
        if (facilitySiteId.HasText())
        {
            facilityFilteredTemplates = allEventTemplates.Where(template => (template.FacilitySiteIds ?? new()).List.Contains(facilitySiteId)).ToList();
        }

        // if facility-specific templates are not found, fallback using the global templates
        if (!facilityFilteredTemplates.Any())
        {
            facilityFilteredTemplates = allEventTemplates.Where(template => !(template.FacilitySiteIds ?? new()).Raw.HasText()).ToList();
        }

        // ======================================== STEP 3. Account for the Account-Specific Templates ============================

        // narrow down the list of templates if the customer context is provided
        var accountFilteredTemplates = new List<EmailTemplateEntity>();
        if (accountId.HasValue)
        {
            accountFilteredTemplates = facilityFilteredTemplates.Where(template => (template.AccountIds ?? new()).List.Contains(accountId.Value)).ToList();
        }

        // if account-specific templates are not found, fallback to using global templates
        if (!accountFilteredTemplates.Any())
        {
            accountFilteredTemplates = facilityFilteredTemplates.Where(template => !(template.AccountIds ?? new()).Raw.HasText()).ToList();
        }

        // ======================================== STEP 4. Pick the Template =====================================================

        // chronological order as a precaution
        var template = accountFilteredTemplates.MinBy(t => t.CreatedAt);
        if (template is null)
        {
            throw new($"Could not resolve email template entity for '{emailTemplateKey}' and facility '{facilitySiteId ?? "All"}'");
        }

        // fetch the template event for the template
        var templateEvent = await _emailTemplateEventProvider.GetById(template.EventId);
        if (templateEvent is null)
        {
            throw new($"Could not resolve email template event entity for '{template.EventName}': {template.EventId}");
        }

        // link them
        template.EmailTemplateEvent = templateEvent;

        return template;
    }

    private async ValueTask<string> RenderSubject(EmailTemplateProcessingContext context)
    {
        var escapedTemplateSource = EscapeTemplateSource(context.EmailTemplate.Subject);
        var body = ReplaceUiTokensWithRazorTokens(context, escapedTemplateSource);
        var dataObject = await GetTemplateDataObject(context);
        return await _emailTemplateRenderer.RenderTemplate(context.EmailTemplate.Name, body, dataObject);
    }

    private async ValueTask<string> RenderBody(EmailTemplateProcessingContext context)
    {
        var escapedTemplateSource = EscapeTemplateSource(context.EmailTemplate.Body);
        var body = ReplaceUiTokensWithRazorTokens(context, escapedTemplateSource);
        var dataObject = await GetTemplateDataObject(context);
        return await _emailTemplateRenderer.RenderTemplate(context.EmailTemplate.Name, body, dataObject);
    }

    protected virtual void SetBccMailAddresses(EmailTemplateProcessingContext context)
    {
        AddAddressesToCollection(context.MailMessage.Bcc, context.EmailTemplate.CustomBccEmails);
        AddAddressesToCollection(context.MailMessage.Bcc, context.TemplateDeliveryRequest.BccRecipients);
        if (context.EmailTemplate.BccType == EmailTemplateBccType.FacilityMainEmail && context.CurrentFacility != null)
        {
            AddAddressesToCollection(context.MailMessage.Bcc, context.CurrentFacility.AdminEmail);
        }
    }

    protected virtual void SetCcMailAddresses(EmailTemplateProcessingContext context)
    {
        AddAddressesToCollection(context.MailMessage.CC, context.TemplateDeliveryRequest.CcRecipients);
        if (context.EmailTemplate.CcType == EmailTemplateCcType.FacilityMainEmail && context.CurrentFacility != null)
        {
            AddAddressesToCollection(context.MailMessage.CC, context.CurrentFacility.AdminEmail);
        }
    }

    protected virtual void SetFromMailAddress(EmailTemplateProcessingContext context)
    {
        if (context.EmailTemplate.UseCustomSenderEmail == true &&
            context.EmailTemplate.SenderEmail.HasText())
        {
            context.MailMessage.From = new(context.EmailTemplate.SenderEmail);
        }
        else if (context.CurrentFacility?.AdminEmail.HasText() == true)
        {
            context.MailMessage.From = new(context.CurrentFacility.AdminEmail);
        }
    }

    protected virtual void SetToMailAddresses(EmailTemplateProcessingContext context)
    {
        AddAddressesToCollection(context.MailMessage.To, context.TemplateDeliveryRequest.Recipients);
    }

    protected virtual void SetReplyToMailAddresses(EmailTemplateProcessingContext context)
    {
        AddAddressesToCollection(context.MailMessage.ReplyToList, context.EmailTemplate.CustomReplyEmail);
        if (context.EmailTemplate.ReplyType == EmailTemplateReplyType.FacilityMainEmail && context.CurrentFacility != null)
        {
            AddAddressesToCollection(context.MailMessage.ReplyToList, context.CurrentFacility.AdminEmail);
        }
    }

    private void AddAddressesToCollection(MailAddressCollection collection, string addressList)
    {
        if (addressList.HasText())
        {
            collection.Add(addressList.Replace(';', ','));
        }
    }

    protected abstract ValueTask<object> GetTemplateDataObject(EmailTemplateProcessingContext context);

    protected virtual ValueTask AddAttachments(EmailTemplateProcessingContext context)
    {
        return ValueTask.CompletedTask;
    }

    private string ReplaceUiTokensWithRazorTokens(EmailTemplateProcessingContext context, string templateSource)
    {
        var templateEvent = context.EmailTemplate.EmailTemplateEvent;
        var tokens = templateEvent?.Fields?.Select(field => (field.UiToken, field.RazorToken)).ToList() ?? new();

        var result = templateSource ?? string.Empty;
        foreach (var (uiToken, razorToken) in tokens)
        {
            result = result.Replace(uiToken, razorToken);
        }

        return result;
    }

    private string EscapeTemplateSource(string templateSource)
    {
        return templateSource?.Replace("@", "@@");
    }
}
