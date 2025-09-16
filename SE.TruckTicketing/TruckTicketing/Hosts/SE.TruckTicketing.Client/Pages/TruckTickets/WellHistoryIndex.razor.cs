using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Contracts.Enums;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class WellHistoryIndex : BaseTruckTicketingComponent
{
    private PagableGridView<SalesLine> _grid;

    private bool _isLoading;

    private SearchResultsModel<SalesLine, SearchCriteriaModel> _results = new();

    [Inject]
    private IServiceProxyBase<SalesLine, Guid> SalesLineService { get; set; }

    [Parameter]
    public Guid? SourceLocationId { get; set; }

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

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
        await _grid.ReloadGrid();
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    private async Task LoadSalesLines(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        criteria.AddFilterIf(SourceLocationId.HasValue, nameof(SalesLine.SourceLocationId), SourceLocationId);
        criteria.AddFilter(nameof(SalesLine.IsAdditionalService), false);
        criteria.AddFilter(nameof(SalesLine.IsCutLine), false);
        criteria.AddFilter(nameof(SalesLine.TruckTicketDate), new CompareModel
        {
            Operator = CompareOperators.gte,
            Value = DateTime.Today.AddDays(-30),
        });

        if (!criteria.OrderBy.HasText())
        {
            criteria.OrderBy = nameof(SalesLine.TruckTicketDate);
            criteria.SortOrder = SortOrder.Desc;
        }

        _results = await SalesLineService.Search(criteria) ?? _results;
        _isLoading = false;
        StateHasChanged();
    }
}
