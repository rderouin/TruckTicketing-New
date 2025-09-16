using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.Facilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels.Facilities;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.Facilities;

public partial class FacilityDetailsPage : BaseTruckTicketingComponent
{
    //private EditContext _editContext;
    private readonly string EmailValidationPattern = "^((?!\\.)[\\w-_.]*[^.])(@[\\w-]+)(\\.\\w+(\\.\\w+)?[^.\\W])$";

    private FacilityServiceGrid _facilityServiceGrid;

    protected RadzenTemplateForm<FacilityDetailsViewModel> _form;

    private bool _isLoading;

    private bool _isSaving;

    private Response<Facility> _response;

    private Dictionary<StateProvince, string> _stateProvinceData = new();

    private FacilityDetailsViewModel _viewModel = new(new());

    private IDictionary<string, Dictionary<StateProvince, string>> _stateProvinceDataByCategory => DataDictionary.GetDataDictionaryByCategory<StateProvince>();

    private bool ShowEnableTareWeight => _viewModel.Facility.Type == FacilityType.Lf;

    [Parameter]
    public Guid? Id { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IServiceProxyBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    private IServiceProxyBase<TicketType, Guid> TicketTypeService { get; set; }

    private IEnumerable<Guid> SelectedTicketType { get; set; }

    private IEnumerable<TicketType> TicketTypesData { get; set; } = new List<TicketType>();

    private bool IsSubmitButtonDisabled => !_form.EditContext.IsModified();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _isLoading = true;
        if (Id != default)
        {
            await LoadFacility(Id.Value);
            _stateProvinceData = _stateProvinceDataByCategory[_viewModel.Facility.CountryCode.ToString()];
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
        await LoadTicketType();
    }

    private void ProvinceOnChange()
    {
        _form.EditContext.NotifyFieldChanged(_form.EditContext.Field(nameof(Facility.Province)));
    }

    private void OnOperatingDayCutoffTimeChanged()
    {
        _form.EditContext.NotifyFieldChanged(_form.EditContext.Field(nameof(Facility.OperatingDayCutOffTime)));
    }

    private async Task LoadTicketType()
    {
        var ticketTypes = await TicketTypeService.Search(new()
        {
            PageSize = 30,
        });

        var selectedIndex = new List<Guid>();
        TicketTypesData = ticketTypes == null ? new() : ticketTypes.Results.ToList();
        if (_viewModel.Facility.TicketTypes.Any())
        {
            selectedIndex.AddRange(_viewModel.Facility.TicketTypes.Select(s => s.Id));
            SelectedTicketType = selectedIndex;
        }
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
            NavigationManager.NavigateTo("/facilities");
        }

        _response = response;
    }

    private async Task OpenFacilityServiceDetailsDialog(FacilityService facilityService)
    {
        await _facilityServiceGrid.OpenFacilityServiceDetailsDialog(facilityService);
    }

    private void OnTicketTypeChange()
    {
        _viewModel.Facility.TicketTypes = SelectedTicketType.Any() ? TicketTypesData.Where(s => SelectedTicketType.Contains(s.Id)).ToList() : null;
    }
}
