using System;
using System.Collections.Generic;
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

public partial class FacilityIndex : BaseRazorComponent
{
    private PagableGridView<Facility> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private SearchResultsModel<Facility, SearchCriteriaModel> _results = new();

    private Guid? _updatingId;

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

    private async Task LoadData(SearchCriteriaModel current)
    {
        _isLoading = true;
        _results = await FacilityService.Search(current) ?? _results;
        _isLoading = false;
    }

    private async Task UpdateIsActiveState(Facility facility, bool isActive)
    {
        _updatingId = facility.Id;
        _isLoading = true;
        var response = await FacilityService.Patch(facility.Id, new Dictionary<string, object> { { nameof(Facility.IsActive), isActive } });
        _isLoading = false;

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Status change successful.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Status change unsuccessful.");
            facility.IsActive = !isActive;
        }

        _updatingId = null;
    }
}
