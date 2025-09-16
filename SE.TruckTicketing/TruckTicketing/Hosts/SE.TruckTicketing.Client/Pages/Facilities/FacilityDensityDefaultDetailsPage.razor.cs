using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels.Facilities;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.Facilities;

public partial class FacilityDensityDefaultDetailsPage : BaseTruckTicketingComponent
{
    protected RadzenTemplateForm<FacilityDetailsViewModel> _form;

    private bool _isLoading;

    private bool _isSaving;

    private Response<Facility> _response;

    private FacilityDetailsViewModel _viewModel = new(new());

    [Parameter]
    public Guid? Id { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IServiceProxyBase<Facility, Guid> FacilityService { get; set; }


    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _isLoading = true;
        if (Id != default)
        {
            await LoadFacility(Id.Value);
        }
        else
        {
            await LoadFacility();
        }

        _isLoading = false;
    }

    private async Task LoadFacility(Guid? id = null)
    {
        var facility = id is null ? new() : await FacilityService.GetById(id.Value);
        _viewModel = new(facility);
    }

    private async Task OnHandleSubmit()
    {
        _isSaving = true;
        _viewModel.CleanUpPrimitiveCollections();

        var response = _viewModel.IsNew ? await FacilityService.Create(_viewModel.Facility) : await FacilityService.Update(_viewModel.Facility);

        _isSaving = false;

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: _viewModel.SubmitSuccessNotificationMessage);
            NavigationManager.NavigateTo("/facility-density-defaults");
        }

        _response = response;
    }
}
