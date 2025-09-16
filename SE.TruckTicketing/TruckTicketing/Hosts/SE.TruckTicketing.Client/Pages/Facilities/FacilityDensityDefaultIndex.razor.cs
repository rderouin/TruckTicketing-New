using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.Facilities;

public partial class FacilityDensityDefaultIndex : BaseRazorComponent
{
    private PagableGridView<Facility> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private SearchResultsModel<Facility, SearchCriteriaModel> _results = new();


    [Inject]
    private IServiceBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private async Task LoadData(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        criteria.Filters[nameof(Facility.ShowConversionCalculator)] = true;
        _results = await FacilityService.Search(criteria) ?? _results;
        _isLoading = false;
    }
}
