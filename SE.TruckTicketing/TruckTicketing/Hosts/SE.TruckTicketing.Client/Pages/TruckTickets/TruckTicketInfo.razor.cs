using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Contracts.Enums;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketInfo : BaseTruckTicketingComponent
{
    private Facility _facility;

    private string _facilityName;

    private TridentApiDropDownDataGrid<FacilityServiceSubstanceIndex, Guid?> _facilityServiceDropDown;

    private MaterialApprovalOperatorNotesAcknowledgmentDialog _materialApprovalAcknowledgment;

    private MaterialApprovalDropDown<Guid?> _materialApprovalDropDown;

    private Guid? _sourceLocation;

    private SourceLocationDropDown<Guid> _sourceLocationDropDown;

    private EventCallback HandleConversionCalculatorCancel => new(this, () => DialogService.Close());

    private EventCallback HandleConversionCalculatorCutUpdate =>
        new(this, () =>
                  {
                      StateHasChanged();
                      DialogService.Close();
                  });

    [CascadingParameter(Name = "TruckTicket")]
    public TruckTicket Model { get; set; }

    [CascadingParameter(Name = "TruckTicketRefresh")]
    public EventCallback Refresh { get; set; }

    [Inject]
    private IServiceProxyBase<TruckTicketWellClassification, Guid> TruckTicketWellClassificationService { get; set; }

    [Inject]
    private IServiceProxyBase<TruckTicketTareWeight, Guid> TruckTicketTareWeightService { get; set; }

    [Inject]
    private IServiceProxyBase<Facility, Guid> FacilityService { get; set; }

    [Parameter]
    public EventCallback LoadPreviewSalesLines { get; set; }

    private bool ShowMaterialApproval => _facility?.Type == FacilityType.Lf || (_facility?.ShowMaterialApproval ?? false);

    protected override void OnParametersSet()
    {
        SetPropertyValue(ref _facilityName, Model?.FacilityName, new(this, LoadFacility));
        SetPropertyValue(ref _sourceLocation, Model?.SourceLocationId, new(this, LoadSourceLocation));
    }

    private async Task LoadFacility()
    {
        if (Model.FacilityId == Guid.Empty)
        {
            _facility = default;
            return;
        }

        _facility = await FacilityService.GetById(Model.FacilityId);
        await _facilityServiceDropDown.Reload();

        if (ShowMaterialApproval)
        {
            await _materialApprovalDropDown.Reload();
        }
    }

    private async Task LoadSourceLocation()
    {
        if (Model.SourceLocationId == Guid.Empty)
        {
            return;
        }

        if (_sourceLocationDropDown.LoadedItems.All(item => item.Id != Model.SourceLocationId))
        {
            await _sourceLocationDropDown.Reload();
        }

        if (ShowMaterialApproval)
        {
            await _materialApprovalDropDown.Reload();
        }
    }

    private void HandleFacilityLoad(Facility facility)
    {
        _facility = facility;
    }

    private async Task HandleFacilityChange(Facility facility)
    {
        _facility = facility;
        Model.FacilityName = facility.Name;
        Model.FacilityId = facility.Id;
        Model.FacilityServiceId = default;
        Model.FacilityType = facility.Type;
        Model.CountryCode = facility.CountryCode;

        await _facilityServiceDropDown.Reload();
        await Task.WhenAll(TryAutoPopulateWellClassification(), TryAutoPopulateMaterialApproval(), TryAutoPopulateTareWeight());
        await LoadPreviewSalesLines.InvokeAsync();
    }

    private async Task HandleSourceLocationChange(SourceLocation sourceLocation)
    {
        Model.SourceLocationId = sourceLocation.Id;
        Model.SourceLocationFormatted = sourceLocation.FormattedIdentifier;
        Model.SourceLocationName = sourceLocation.SourceLocationName;
        Model.GeneratorId = sourceLocation.GeneratorId;
        Model.GeneratorName = sourceLocation.GeneratorName;

        await TryAutoPopulateWellClassification();
        await TryAutoPopulateMaterialApproval();
        await LoadPreviewSalesLines.InvokeAsync();
    }

    private async Task HandleWellClassificationChange()
    {
        await LoadPreviewSalesLines.InvokeAsync();
    }

    private async Task HandleMaterialApprovalChange(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        await _materialApprovalAcknowledgment.OpenDialog(materialApproval);
    }

    private void HandleMaterialApprovalAccept(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        SetMaterialApprovalDependencies(materialApproval);
        StateHasChanged();
    }

    private void HandleMaterialApprovalAcknowledgementDecline()
    {
        Model.MaterialApprovalId = default;
        SetMaterialApprovalDependencies(null);
        StateHasChanged();
    }

    private void SetMaterialApprovalDependencies(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        Model.MaterialApprovalNumber = materialApproval?.MaterialApprovalNumber;
        Model.FacilityServiceSubstanceId = materialApproval?.FacilityServiceSubstanceIndexId;
        Model.FacilityServiceId = materialApproval?.FacilityServiceId;
        Model.WasteCode = materialApproval?.WasteCodeName;
        Model.ServiceType = materialApproval?.FacilityServiceName;
        Model.SubstanceName = materialApproval?.SubstanceName;
        Model.SubstanceId = materialApproval?.SubstanceId ?? default;
    }

    private void HandleFacilityServiceLoading(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(FacilityServiceSubstanceIndex.FacilityId)] = Model.FacilityId;
        criteria.Filters[nameof(FacilityServiceSubstanceIndex.IsAuthorized)] = true;
    }

    private async Task HandleFacilityServiceChange(FacilityServiceSubstanceIndex index)
    {
        Model.SubstanceId = index.SubstanceId;
        Model.SubstanceName = index.Substance;
        Model.WasteCode = index.WasteCode;
        Model.FacilityServiceId = index.FacilityServiceId;
        Model.ServiceTypeId = index.ServiceTypeId;
        Model.ServiceType = index.ServiceTypeName;
        await LoadPreviewSalesLines.InvokeAsync();
    }

    private void HandleLoadDateChange(DateTimeOffset? value)
    {
        Model.LoadDate = value;
        SynchronizeLoadDateTimeIn(Model.LoadDate ?? DateTimeOffset.Now);
    }

    private async Task HandleTruckingCompanyChange(Account company)
    {
        Model.TruckingCompanyName = company.Name;
        await TryAutoPopulateTareWeight();
    }

    private async Task HandleTruckNumberChange()
    {
        await TryAutoPopulateTareWeight();
    }

    private async Task HandleTrailerNumberChange()
    {
        await TryAutoPopulateTareWeight();
    }

    private async Task OpenConversionCalculator()
    {
        await DialogService.OpenAsync<DensityConversionCalculator>("Conversion Calculator", new()
        {
            { nameof(DensityConversionCalculator.TruckTicket), Model },
            { nameof(DensityConversionCalculator.OnCancel), HandleConversionCalculatorCancel },
            { nameof(DensityConversionCalculator.OnCutsUpdate), HandleConversionCalculatorCutUpdate },
        }, new()
        {
            Width = "65%",
            CloseDialogOnOverlayClick = false,
        });
    }

    private void SynchronizeLoadDateTimeIn(DateTimeOffset? timeIn)
    {
        if (Model.LoadDate.HasValue)
        {
            Model.LoadDate = Model.LoadDate.Value.AddHours(timeIn?.Hour ?? 0).AddMinutes(timeIn?.Minute ?? 0);
        }
    }

    private async Task TryAutoPopulateWellClassification()
    {
        if (Model.FacilityId == default || Model.SourceLocationId == default)
        {
            return;
        }

        var criteria = new SearchCriteriaModel
        {
            PageSize = 1,
            SortOrder = SortOrder.Desc,
            OrderBy = nameof(TruckTicketWellClassification.Date),
            Filters = new()
            {
                { nameof(TruckTicketWellClassification.FacilityId), Model.FacilityId },
                { nameof(TruckTicketWellClassification.SourceLocationId), Model.SourceLocationId },
            },
        };

        var response = await TruckTicketWellClassificationService.Search(criteria);
        var index = response?.Results?.FirstOrDefault();

        if (index is not null)
        {
            Model.WellClassification = index.WellClassification;
        }
    }

    private async Task TryAutoPopulateMaterialApproval()
    {
        if (_materialApprovalDropDown is null)
        {
            return;
        }

        await _materialApprovalDropDown.Reload();
        var materialApprovals = _materialApprovalDropDown.LoadedItems.ToArray();
        SetMaterialApprovalDependencies(materialApprovals.Length == 1 ? materialApprovals[0] : null);
        StateHasChanged();
    }

    private async Task LoadSampledSwitchChange(bool value)
    {
        Model.RequireSample = !value;
        await Refresh.InvokeAsync();
    }

    private async Task TryAutoPopulateTareWeight()
    {
        if (Model.Status == TruckTicketStatus.New && Model.TareWeight == 0 && Model.FacilityId != default && Model.TruckNumber.HasText() && Model.TruckingCompanyName.HasText())
        {
            var criteria = new SearchCriteriaModel
            {
                PageSize = 1,
                SortOrder = SortOrder.Desc,
                OrderBy = nameof(TruckTicketTareWeight.LoadDate),
                Filters = new()
                {
                    { nameof(TruckTicketTareWeight.FacilityId), Model.FacilityId },
                    { nameof(TruckTicketTareWeight.TruckingCompanyName), Model.TruckingCompanyName },
                    { nameof(TruckTicketTareWeight.TruckNumber), Model.TruckNumber },
                    { nameof(TruckTicketTareWeight.TrailerNumber), Model.TrailerNumber ?? string.Empty },
                    { nameof(TruckTicketTareWeight.IsActivated), true },
                },
            };

            var response = await TruckTicketTareWeightService.Search(criteria);
            var index = response?.Results?
               .FirstOrDefault();

            if (index is not null)
            {
                Model.TareWeight = index.TareWeight;
            }
        }
    }

    public async Task OnWellClassificationChange()
    {
        await LoadPreviewSalesLines.InvokeAsync();
    }
}
