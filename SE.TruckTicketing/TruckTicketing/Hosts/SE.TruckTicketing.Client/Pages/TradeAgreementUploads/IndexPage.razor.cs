using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.TradeAgreementUploads;

public partial class IndexPage : BaseTruckTicketingComponent
{
    private PagableGridView<TradeAgreementUpload> _grid;

    private bool _isLoading;

    private SearchResultsModel<TradeAgreementUpload, SearchCriteriaModel> _tradeAgreementUploads = new();

    private readonly Dictionary<string, TradeAgreementUpload> _uploads = new();

    [Inject]
    private ITradeAgreementUploadService TradeAgreementUploadService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    protected async Task LoadTradeAgreementUploads(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        _tradeAgreementUploads = await TradeAgreementUploadService.Search(criteria) ?? _tradeAgreementUploads;
        _isLoading = false;
        StateHasChanged();
    }

    protected async Task<string> GetTradeAgreementUploadUrl(FileUploadContext context)
    {
        var tradeAgreement = await TradeAgreementUploadService.GetUploadUrl();
        tradeAgreement.OriginalFileName = context.File.Name;
        _uploads[context.File.Name] = tradeAgreement;
        return tradeAgreement.Uri;
    }

    protected async Task HandleTradeAgreementUploadCompletion(IEnumerable<FileUploadContext> contexts)
    {
        var context = contexts.First();

        var response = await TradeAgreementUploadService.Create(_uploads[context.File.Name]);

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Success", "The TA file was successfully uploaded.");
            await _grid.ReloadGrid();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "An error occurred while uploading the TA file.");
        }
    }
}
