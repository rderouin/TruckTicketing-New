using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class AttachmentsTab : BaseTruckTicketingComponent
{
    private SearchResultsModel<LoadConfirmationAttachment, SearchCriteriaModel> _attachments = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<LoadConfirmationAttachment>(),
    };

    private Dictionary<string, LoadConfirmationAttachment> _uploads = new();

    [CascadingParameter]
    public LoadConfirmation Model { get; set; }

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    public ILoadConfirmationService LoadConfirmationService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    private PagableGridView<LoadConfirmationAttachment> _loadConfirmationAttachmentGrid { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadAttachments();
    }

    private async Task HandleAttachmentDownload(LoadConfirmationAttachment attachment)
    {
        var uri = await LoadConfirmationService.GetAttachmentDownloadUrl(Model.Key, attachment.Id);
        await JsRuntime.InvokeVoidAsync("open", uri, "_blank");
    }

    private void LoadAttachments()
    {
        _attachments = new(Model?.Attachments ?? Enumerable.Empty<LoadConfirmationAttachment>());
    }

    private async Task<string> GetAttachmentUploadUri(FileUploadContext context)
    {
        var attachmentResponse = await LoadConfirmationService.GetAttachmentUploadUrl(Model.Key, context.File.Name);
        if (!attachmentResponse.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: $"Failed to upload {context.File.Name}.");
            return null;
        }

        _uploads[attachmentResponse.Model.FileName] = attachmentResponse.Model;
        return attachmentResponse.Model.Uri;
    }

    private async Task ToggleAttachmentInclusion(LoadConfirmationAttachment attachment, bool isIncluded)
    {
        Model.RequiresInvoiceDocumentRegeneration = true;
        
        var response = await LoadConfirmationService.Update(Model);
        if (!response.IsSuccessStatusCode)
        {
            attachment.IsIncludedInInvoice = !attachment.IsIncludedInInvoice;
        }

        StateHasChanged();
    }

    private async Task HandleUploadComplete(IEnumerable<FileUploadContext> contexts)
    {
        var attachments = contexts.Where(c => !c.UploadError).Select(context =>
                                          {
                                              var attachment = _uploads[context.File.Name];
                                              attachment.ContentType = context.File.ContentType;
                                              attachment.IsIncludedInInvoice = true;
                                              attachment.AttachmentOrigin = LoadConfirmationAttachmentOrigin.Manual;
                                              return attachment;
                                          }).Where(attachment => Model.Attachments.All(existing => existing.FileName != attachment.FileName)).ToList();

        if (attachments.Any())
        {
            Model.Attachments.AddRange(attachments);
            Model.RequiresInvoiceDocumentRegeneration = true;
            var response = await LoadConfirmationService.Patch(Model.Id, new Dictionary<string, object> { [nameof(Model.Attachments)] = Model.Attachments });

            if (response.IsSuccessStatusCode)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Success", $"{attachments.Count} attachments uploaded.");
                await _loadConfirmationAttachmentGrid.ReloadGrid();
                StateHasChanged();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", "We could not link the uploaded attachments to the load confirmation.");
            }
        }
    }

    private async Task HandleAttachmentRemove(LoadConfirmationAttachment attachment)
    {
        var confirmation = await DialogService.Confirm("Are you sure you want to remove this attachment?", options: new()
        {
            OkButtonText = "Proceed",
            CancelButtonText = "Cancel",
        });

        if (confirmation != true)
        {
            return;
        }

        var response = await LoadConfirmationService.RemoveAttachment(Model.Key, attachment.Id);
        if (!response.IsSuccessStatusCode)
        {
            var message = "Unable to remove the attachment.";
            throw new InvalidOperationException(message);
        }

        var loadConfirmation = response.Model;
        Model.Attachments = loadConfirmation?.Attachments;
        await _loadConfirmationAttachmentGrid.ReloadGrid();
        StateHasChanged();
    }
}
