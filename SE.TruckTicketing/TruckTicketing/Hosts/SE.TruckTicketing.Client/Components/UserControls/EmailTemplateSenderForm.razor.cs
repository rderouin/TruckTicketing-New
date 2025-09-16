using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Models.Email;
using SE.TruckTicketing.UI.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class EmailTemplateSenderForm : BaseTruckTicketingComponent
{
    private readonly Dictionary<FileUploadContext, AdHocAttachmentModel> _attachmentModels = new();

    private EmailTemplateEvent EmailTemplateEvent { get; set; }

    private FormModel Model { get; set; }

    private bool ShowConfirmation { get; set; }

    private bool IsSending { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<EmailTemplateDeliveryRequestModel> OnRequestDelivering { get; set; }

    [Parameter]
    public EventCallback OnRequestDelivered { get; set; }

    [Parameter]
    public string EventName { get; set; }

    [Inject]
    private IServiceProxyBase<EmailTemplateEvent, Guid> EmailTemplateEventService { get; set; }

    [Inject]
    private IEmailTemplateService EmailTemplateService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Model = new();
        await LoadEmailTemplate(EventName);
    }

    private async Task LoadEmailTemplate(string templateEventName)
    {
        var criteria = new SearchCriteriaModel { PageSize = 1 };
        criteria.AddFilter(nameof(EmailTemplateEvent.Name), templateEventName);

        var search = await EmailTemplateEventService.Search(criteria);
        EmailTemplateEvent = search.Results.FirstOrDefault();
    }

    protected void HandleConfirmationDenied()
    {
        ShowConfirmation = false;
    }

    protected async Task HandleSubmit()
    {
        if (Model.IsClean())
        {
            ShowConfirmation = true;
        }
        else
        {
            await SendRequest();
        }
    }

    protected async Task<string> GetAttachmentUploadUri(FileUploadContext context)
    {
        var attachment = await EmailTemplateService.GetAdhocAttachmentUploadUri(new()
        {
            FileName = context.File.Name,
            ContentType = context.File.ContentType,
            Size = context.File.Size,
            RequestId = Application.User.Principal.Identity!.Name + '-' + DateTime.UtcNow.ToString("hhmmsstz"),
        });

        _attachmentModels[context] = attachment;

        return attachment.Uri;
    }

    protected void SetAttachments(IEnumerable<FileUploadContext> contexts)
    {
        Model.Attachments = contexts.Where(context => _attachmentModels.ContainsKey(context))
                                    .Select(context => _attachmentModels[context])
                                    .ToList();
    }

    private async Task SendRequest()
    {
        ShowConfirmation = false;

        var request = Model.ToEmailDeliveryRequest();
        request.TemplateKey = EventName;

        if (OnRequestDelivering.HasDelegate)
        {
            await OnRequestDelivering.InvokeAsync(request);
        }

        IsSending = true;
        var response = await EmailTemplateService.SendEmail(request);
        IsSending = false;

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success,
                                       "Success",
                                       "We've delivered your email request.");

            if (OnRequestDelivered.HasDelegate)
            {
                await OnRequestDelivered.InvokeAsync();
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error,
                                       "Error",
                                       "Something went wrong while trying to deliver your email request.");
        }
    }

    protected class FormModel
    {
        public string ToRecipients { get; set; }

        public string CcRecipients { get; set; }

        public string BccRecipients { get; set; }

        public string CustomNote { get; set; }

        public List<AdHocAttachmentModel> Attachments { get; set; }

        public bool IsClean()
        {
            var freshModel = new FormModel();
            return this.ToJson() == freshModel.ToJson();
        }

        public EmailTemplateDeliveryRequestModel ToEmailDeliveryRequest()
        {
            return new()
            {
                Recipients = ToRecipients,
                CcRecipients = CcRecipients,
                BccRecipients = BccRecipients,
                AdHocNote = CustomNote,
                AdHocAttachments = Attachments,
            };
        }
    }
}
