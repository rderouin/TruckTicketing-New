using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components.Grid;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Components.SourceLocations;

public partial class SourceLocationWellClassificationUsageIndex : BaseTruckTicketingComponent
{
    private PagableGridView<TruckTicketWellClassification> _grid;

    private bool _isLoading;

    private SearchResultsModel<TruckTicketWellClassification, SearchCriteriaModel> _results = new();

    [Parameter]
    public Guid? SourceLocationId { get; set; }

    [Inject]
    private IServiceProxyBase<TruckTicketWellClassification, Guid> WellClassificationUsageService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    private async Task LoadData(SearchCriteriaModel criteria)
    {
        if (SourceLocationId == default)
        {
            return;
        }

        _isLoading = true;
        criteria.Filters ??= new();
        criteria.Filters[nameof(TruckTicketWellClassification.SourceLocationId)] = SourceLocationId;
        criteria.OrderBy = nameof(TruckTicketWellClassification.Date);
        criteria.SortOrder = SortOrder.Desc;
        _results = await WellClassificationUsageService.Search(criteria) ?? _results;
        _isLoading = false;
    }

    private async Task HandleDelete(TruckTicketWellClassification index)
    {
        _isLoading = true;

        var response = await WellClassificationUsageService.Delete(index);

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Success", "Well classification usage rule removed.");
            await _grid.ReloadGrid();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error,
                                       "Error",
                                       "Unable to remove well classification usage rule.");
        }

        _isLoading = false;
    }
}
