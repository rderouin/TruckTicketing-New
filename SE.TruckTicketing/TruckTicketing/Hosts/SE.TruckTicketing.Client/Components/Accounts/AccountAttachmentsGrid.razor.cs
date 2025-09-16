using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class AccountAttachmentsGrid : BaseRazorComponent
{
    private SearchResultsModel<AccountAttachment, SearchCriteriaModel> _attachments = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<AccountAttachment>(),
    };

    [Parameter]
    public Account Model { get; set; }

    [Inject]
    public IBlazorDownloadFileService DownloadFileService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [Inject]
    private IAccountService AccountService { get; set; }

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

    private Task LoadAttachments()
    {
        _attachments = new(Model.Attachments);
        return Task.CompletedTask;
    }

    private async Task OpenViewDialog(AccountAttachment attachment)
    {
        var uri = await AccountService.GetAttachmentDownloadUri(Model.Id, attachment);
        if (!string.IsNullOrWhiteSpace(uri))
        {
            NavigationManager.NavigateTo($"{uri}");
        }
    }
}
