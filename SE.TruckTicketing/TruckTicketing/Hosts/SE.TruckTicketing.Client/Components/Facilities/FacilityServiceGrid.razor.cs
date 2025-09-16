using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.Facilities;

public partial class FacilityServiceGrid : BaseTruckTicketingComponent
{
    private FacilityServiceDetailsViewModel _detailsViewModel;

    private Guid _facilityId = Guid.NewGuid();

    private PagableGridView<FacilityService> _grid;

    private bool _isLoading;

    private SearchResultsModel<FacilityService, SearchCriteriaModel> _results = new();

    [Inject]
    public IServiceProxyBase<FacilityService, Guid> FacilityServiceProxy { get; set; }

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Parameter]
    public Facility Facility { get; set; }

    [Parameter]
    public EventCallback<FacilityService> OnActiveToggle { get; set; }

    private EventCallback HandleFacilityServicesDialogCancel => new(this, () => DialogService.Close());

    private EventCallback<FacilityService> HandleFacilityServicesDialogSubmit => new(this, OnSubmit);

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        SetPropertyValue(ref _facilityId, Facility.Id, new(this, async () => await Refresh()));
    }

    private async Task LoadData(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(FacilityService.FacilityId)] = Facility.Id;
        _results = await FacilityServiceProxy.Search(criteria) ?? _results;
    }

    public async Task Refresh()
    {
        await _grid.ReloadGrid();
    }

    private async Task HandleIsActiveToggle(FacilityService facilityService)
    {
        if (OnActiveToggle.HasDelegate)
        {
            await FacilityServiceProxy.Patch(Facility.Id, new Dictionary<string, object> { { nameof(FacilityService.IsActive), facilityService.IsActive } });
            await OnActiveToggle.InvokeAsync(facilityService);
        }
    }

    private async Task UpdateIsActiveState(FacilityService facilityService, bool isActive)
    {
        _isLoading = true;
        var response = await FacilityServiceProxy.Patch(facilityService.Id, new Dictionary<string, object> { { nameof(FacilityService.IsActive), isActive } });
        _isLoading = false;

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Status change successful.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Status change unsuccessful.");
            facilityService.IsActive = !isActive;
        }
    }

    public async Task OpenFacilityServiceDetailsDialog(FacilityService facilityService = null)
    {
        _detailsViewModel = new(facilityService ?? new())
        {
            FacilityService =
            {
                SiteId = Facility.SiteId,
                FacilityId = Facility.Id,
            },
        };

        await DialogService.OpenAsync<FacilityServiceDetails>(facilityService.Id == default ? "Creating Facility Service" : "Updating Facility Service " + facilityService.FacilityServiceNumber,
                                                              new()
                                                              {
                                                                  { nameof(FacilityServiceDetails.Model), _detailsViewModel },
                                                                  { nameof(FacilityServiceDetails.OnCancel), HandleFacilityServicesDialogCancel },
                                                                  { nameof(FacilityServiceDetails.OnSubmit), HandleFacilityServicesDialogSubmit },
                                                                  { nameof(FacilityServiceDetails.LegalEntityId), Facility.LegalEntityId },
                                                              }, new()
                                                              {
                                                                  Width = "75%",
                                                              });
    }

    private async Task OnSubmit(FacilityService model)
    {
        if (model.Id == default)
        {
            model.IsActive = true;
        }

        var notificationMessage = model.Id == default ? "Facility service added." : "Facility service updated.";

        var response = await FacilityServiceProxy.Create(model);

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Success", notificationMessage);

            DialogService.Close();

            await _grid.ReloadGrid();
        }
        else if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "Failed to save facility service.");
        }

        _detailsViewModel.Response = response;
    }
}
