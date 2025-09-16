using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using Radzen;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketAttachments : BaseTruckTicketingComponent
{
    private SearchResultsModel<TruckTicketAttachment, SearchCriteriaModel> _attachments = new();

    private PagableGridView<TruckTicketAttachment> _grid;

    private Dictionary<string, TruckTicketAttachment> _uploads = new();

    public TruckTicket Model => ViewModel.TruckTicket;

    [Inject]
    public ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    public IBlazorDownloadFileService DownloadFileService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    [Inject]
    public TruckTicketExperienceViewModel ViewModel { get; set; }

    public override void Dispose()
    {
        ViewModel.Initialized -= StateChange;
    }

    protected override void OnInitialized()
    {
        ViewModel.Initialized += StateChange;
    }

    protected async Task StateChange()
    {
        await LoadAttachments();
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAttachments();
    }

    private async Task LoadAttachments()
    {
        _attachments = new(Model?.Attachments.Where(attachment => attachment.IsUploaded).ToList() ?? new());
        await Task.CompletedTask;
    }

    private async Task HandleAttachmentDownload(TruckTicketAttachment attachment)
    {
        var uriResponse = await TruckTicketService.GetAttachmentDownloadUrl(Model.Key.Id, attachment.Id);
        if (!uriResponse.IsSuccessStatusCode)
        {
            return;
        }

        var uri = JToken.Parse(uriResponse.Model).ToObject<string>();
        var response = await HttpClientFactory.CreateClient().GetAsync(uri);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        await using var stream = await response.Content.ReadAsStreamAsync();

        await DownloadFileService.DownloadFile(attachment.File, stream, contentType);
    }

    private async Task HandleAttachmentRemove(TruckTicketAttachment attachment)
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

        var truckTicketResponse = await TruckTicketService.RemoveAttachment(Model.Key, attachment.Id);
        if (!truckTicketResponse.IsSuccessStatusCode)
        {
            var message = "Unable to remove the attachment.";
            BaseLogger.LogError(message);
            throw new InvalidOperationException(message);
        }

        var truckTicket = truckTicketResponse.Model;
        Model.Attachments = truckTicket?.Attachments;
        await LoadAttachments();
    }

    private async Task<string> GetAttachmentUploadUrl(FileUploadContext context)
    {
        var uploadModelResponse = await TruckTicketService.GetAttachmentUploadUrl(Model.Key.Id, context.File.Name, context.File.ContentType);
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
            if (_uploads.TryGetValue(context.File.Name, out var attachment))
            {
                var updateTruckTicketResponse = await TruckTicketService.MarkFileUploaded(Model.Key.Id, attachment.Id);
                if (updateTruckTicketResponse.IsSuccessStatusCode)
                {
                    Model.Attachments = updateTruckTicketResponse.Model.Attachments;
                    uploadedCount++;
                }
            }
        }

        if (uploadedCount > 0)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Success", $"{uploadedCount} attachment{(uploadedCount == 1 ? "s" : "")} uploaded.");
            await LoadAttachments();
            await _grid.ReloadGrid();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "Unable to link the uploaded attachments to the truck Ticket.");
        }
    }
}
