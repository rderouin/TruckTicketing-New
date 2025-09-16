using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Newtonsoft.Json.Linq;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public partial class AttachmentsTab : BaseTruckTicketingComponent
{
    private PagableGridView<InvoiceAttachment> _grid;

    private Dictionary<string, InvoiceAttachment> _uploads = new();

    private SearchResultsModel<InvoiceAttachment, SearchCriteriaModel> Attachments => new(Model?.Attachments ?? Enumerable.Empty<InvoiceAttachment>());

    [CascadingParameter]
    public Invoice Model { get; set; }

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    public IInvoiceService InvoiceService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    private async Task HandleAttachmentDownload(InvoiceAttachment attachment)
    {
        // fetch the signed URL
        var uriResponse = await InvoiceService.GetAttachmentDownloadUrl(Model.Key, attachment.Id);
        if (!uriResponse.IsSuccessStatusCode)
        {
            return;
        }

        // fetch data
        var uri = JToken.Parse(uriResponse.Model).ToObject<string>();
        await JsRuntime.InvokeVoidAsync("open", uri, "_blank");
    }

    private async Task<string> GetAttachmentUploadUri(FileUploadContext context)
    {
        // fetch the upload URL
        var uploadModelResponse = await InvoiceService.GetAttachmentUploadUrl(Model.Key, context.File.Name, context.File.ContentType);
        if (!uploadModelResponse.IsSuccessStatusCode)
        {
            var message = "Unable to acquire the download URL.";
            BaseLogger.LogError(message);
            throw new InvalidOperationException(message);
        }

        // keep track of the uploads
        var uploadModel = uploadModelResponse.Model;
        _uploads[context.File.Name] = uploadModel.Attachment;
        return uploadModel.Uri;
    }

    private async Task HandleUploadComplete(IEnumerable<FileUploadContext> contexts)
    {
        var uploadedCount = 0;

        foreach (var context in contexts)
        {
            if (_uploads.TryGetValue(context.File.Name, out var attachment))
            {
                var updatedInvoiceResponse = await InvoiceService.MarkFileUploaded(Model.Key, attachment.Id);
                if (updatedInvoiceResponse.IsSuccessStatusCode)
                {
                    Model.Attachments = updatedInvoiceResponse.Model.Attachments;
                    uploadedCount++;
                }
            }
        }

        if (uploadedCount > 0)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Success", $"{uploadedCount} attachments uploaded.");
            StateHasChanged();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "Unable to link the uploaded attachments to the invoice.");
        }
    }
}
