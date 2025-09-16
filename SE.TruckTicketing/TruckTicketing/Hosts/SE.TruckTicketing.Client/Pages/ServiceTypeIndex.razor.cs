using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages;

public partial class ServiceTypeIndex : BaseTruckTicketingComponent
{
    private PagableGridView<ServiceType> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private SearchResultsModel<ServiceType, SearchCriteriaModel> _results = new();

    private Guid? _updatingId;

    [Inject]
    public IServiceTypeService ServiceTypeService { get; set; }

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
        _results = await ServiceTypeService.Search(current) ?? _results;
        _isLoading = false;
        StateHasChanged();
    }

    private void OpenServiceTypeDialog(Guid? serviceTypeId = null)
    {
        NavigationManager.NavigateTo($"/ServiceType/{serviceTypeId}");
    }

    private async Task OnChange(Guid id, bool isActive)
    {
        _updatingId = id;
        _isLoading = true;
        var response = await ServiceTypeService.Patch(id, new Dictionary<string, object> { { nameof(ServiceType.IsActive), isActive } });
        _isLoading = false;

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Status change successful.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Status change unsuccessful.");
            var serviceType = _results?.Results?.FirstOrDefault(serviceType => serviceType.Id == id);
            if (serviceType is not null)
            {
                serviceType.IsActive = !isActive;
            }
        }

        _updatingId = null;
    }
}
