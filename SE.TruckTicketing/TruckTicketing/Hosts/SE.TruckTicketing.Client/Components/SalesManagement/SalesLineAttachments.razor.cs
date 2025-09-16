using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class SalesLineAttachments : BaseTruckTicketingComponent
{
    private readonly Dictionary<string, TruckTicketAttachment> _uploads = new();

    private Guid? _downloadingAttachmentId;

    [Parameter]
    public SalesLine SalesLine { get; set; }

    [Parameter]
    public EventCallback UploadComplete { get; set; }

    [Inject]
    public ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    public IJSRuntime JsRuntime { get; set; }

    private async Task OpenAttachment(SalesLineAttachment attachment)
    {
        _downloadingAttachmentId = attachment.Id;
        var response = await TruckTicketService.GetAttachmentDownloadUrl(SalesLine.TruckTicketId, attachment.Id);
        _downloadingAttachmentId = default;
        StateHasChanged();
        await JsRuntime.InvokeAsync<object>("open", response.Model.Trim('"'), "_blank");
    }

    private async Task<string> GetAttachmentUploadUrl(FileUploadContext context)
    {
        var uploadModelResponse = await TruckTicketService.GetAttachmentUploadUrl(SalesLine.TruckTicketId, context.File.Name, context.File.ContentType);
        if (!uploadModelResponse.IsSuccessStatusCode)
        {
            var message = "Unable to acquire the download URL.";
            BaseLogger.LogError(message);
            throw new InvalidOperationException(message);
        }

        var uploadModel = uploadModelResponse.Model;
        _uploads[context.File.Name] = uploadModel.Attachment;
        return uploadModel.Uri;
    }

    private async Task HandleAttachmentUploadCompletion(IEnumerable<FileUploadContext> contexts)
    {
        var uploadedCount = 0;

        foreach (var context in contexts)
        {
            if (!_uploads.TryGetValue(context.File.Name, out var attachment))
            {
                continue;
            }

            var updateTruckTicketResponse = await TruckTicketService.MarkFileUploaded(SalesLine.TruckTicketId, attachment.Id);
            if (!updateTruckTicketResponse.IsSuccessStatusCode)
            {
                continue;
            }

            SalesLine.Attachments = updateTruckTicketResponse.Model.Attachments.Select(ttAttachment => new SalesLineAttachment
            {
                Id = ttAttachment.Id,
                Container = ttAttachment.Container,
                File = ttAttachment.File,
                Path = ttAttachment.Path,
            }).ToList();

            uploadedCount++;
        }

        if (uploadedCount > 0)
        {
            await UploadComplete.InvokeAsync();
            NotificationService.Notify(NotificationSeverity.Success, "Success", $"{uploadedCount} attachment{(uploadedCount == 1 ? "s" : "")} uploaded.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "Unable to link the uploaded attachments to the truck Ticket.");
        }
    }
}
