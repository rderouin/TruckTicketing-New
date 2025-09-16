using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class AttachmentsGrid : BaseRazorComponent
{
    private SearchResultsModel<TruckTicketAttachment, SearchCriteriaModel> _attachments = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<TruckTicketAttachment>(),
    };

    [Parameter]
    public TruckTicket Model { get; set; }

    [Parameter]
    public EventCallback<FieldIdentifier> OnContextChange { get; set; }

    [Inject]
    public IBlazorDownloadFileService DownloadFileService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAttachment> NewAttachmentEventCallback { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAttachment> OnAttachmentRemove { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<TruckTicketAttachment> AddAttachmentHandler =>
        new(this, (Func<TruckTicketAttachment, Task>)(async model =>
                                                      {
                                                          DialogService.Close();
                                                          await NewAttachmentEventCallback.InvokeAsync(model);
                                                      }));

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAttachments();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadAttachments();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnContextChange.InvokeAsync(e.FieldIdentifier);
    }

    private Task LoadAttachments()
    {
        _attachments = new(Model.Attachments);
        return Task.CompletedTask;
    }

    private async Task AddAttachment()
    {
        await DialogService.OpenAsync<AttachmentUploadComponent>("Attachment", new()
        {
            { "TruckTicketModel", Model },
            { nameof(AttachmentUploadComponent.AddAttachmentCallback), AddAttachmentHandler },
            { nameof(AttachmentUploadComponent.OnCancel), HandleCancel },
        });
    }

    private async Task OpenViewDialog(TruckTicketAttachment ttAttachment)
    {
        var uriResponse = await TruckTicketService.GetAttachmentDownloadUrl(Model.Key.Id, ttAttachment.Id);
        var response = await HttpClientFactory.CreateClient().GetAsync(uriResponse?.Model);
        await DownloadFileService.DownloadFile(ttAttachment.File, await response.Content.ReadAsStreamAsync(), "application/octet-stream");
    }

    private async Task DeleteAttachment(TruckTicketAttachment attachment)
    {
        if (OnAttachmentRemove.HasDelegate)
        {
            await OnAttachmentRemove.InvokeAsync(attachment);
        }
    }
}
