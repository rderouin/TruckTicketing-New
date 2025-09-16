using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Pages.MaterialApproval;
using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Extensions;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketInfo : BaseTruckTicketingComponent
{
    private NewTruckTicketLoadQuantities _quantitiesComponent;

    private List<PreSetDensityConversionParams> _defaultDensityFactorForSelectedFacilityByMidWeight = new();

    private List<PreSetDensityConversionParams> _defaultDensityFactorForSelectedFacilityByWeight = new();

    private PreSetDensityConversionParams _defaultDensityFactorsByMidWeight;

    private PreSetDensityConversionParams _defaultDensityFactorsByWeight;

    private FacilityDropDown<Guid> _facilityDropDown;

    private TridentApiDropDownDataGrid<FacilityServiceSubstanceIndex, Guid?> _facilityServiceDropDown;

    private SearchCriteriaModel _facilityServiceSearchCriteriaModel;

    //private bool _initialized;

    private MaterialApprovalOperatorNotesAcknowledgmentDialog _materialApprovalAcknowledgment;

    private RadzenDropDown<Guid?> _materialApprovalDropDown;

    private SourceLocationDropDown<Guid> _sourceLocationDropDown;

    private SearchCriteriaModel _sourceLocationSearchCriteriaModel;

    private TridentApiDropDownDataGrid<SpartanProductParameter, Guid> _spartanProductParameterDropDown;

    private SearchCriteriaModel _spartanProductParameterSearchCriteriaModel;

    private AccountsDropDown<Guid> _truckingCompanyDropDown;

    private SearchCriteriaModel _truckingCompanySearchCriteriaModel;

    private bool UseCache => !ViewModel.IsRefresh;

    private bool DisableFacilityServiceSubstanceIndex => ViewModel.Facility?.Type == FacilityType.Lf || ViewModel.TruckTicket.Status is TruckTicketStatus.Approved or TruckTicketStatus.Invoiced;

    private EventCallback HandleConversionCalculatorCancel => new(this, () => DialogService.Close());

    private EventCallback HandleConversionCalculatorCutUpdate =>
        new(this, async () =>
                  {
                      ViewModel.SetCutLabelValidation();
                      DialogService.Close();
                      StateHasChanged();
                      await ViewModel.TriggerWorkflows();
                  });

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private IFacilityServiceSubstanceIndexService FacilityServiceSubstanceIndexService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    public override void Dispose()
    {
        ViewModel.Initialized -= ViewModelOnInitialized;
        ViewModel.StateChanged -= StateChange;
        ViewModel.RefreshTruckTicket -= RefreshTicketData;
    }

    protected override void OnInitialized()
    {
        ViewModel.Initialized += ViewModelOnInitialized;
        ViewModel.StateChanged += StateChange;
        ViewModel.RefreshTruckTicket += RefreshTicketData;
    }

    private async Task ViewModelOnInitialized()
    {
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task RefreshTicketData()
    {
        await ViewModel.SetFacility(ViewModel.Facility);
        if (ViewModel.SourceLocation != null)
        {
            await OnSourceLocationChange(ViewModel.SourceLocation);
        }

        if (DisableFacilityServiceSubstanceIndex || ViewModel.FacilityServiceSubstanceIndex == null ||
            (ViewModel.ShowMaterialApproval && (ViewModel.MaterialApprovals == null || !ViewModel.MaterialApprovals.Any())))
        {
            StateHasChanged();
            return;
        }

        await OnFacilityServiceChange(ViewModel.FacilityServiceSubstanceIndex);

        StateHasChanged();
    }

    private async Task StateChange()
    {
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleMaterialApprovalChange()
    {
        var materialApproval = _materialApprovalDropDown.SelectedItem as Contracts.Models.Operations.MaterialApproval;
        await HandleAnalyticalExpiration(materialApproval);
        await _materialApprovalAcknowledgment.OpenDialog(materialApproval);
        await HandleMaterialApprovalUpdate(materialApproval);
    }

    protected async Task HandleMaterialApprovalAccept(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        await ViewModel.SetMaterialApproval(materialApproval);
        await HandleMaterialApprovalUpdate(materialApproval);
        StateHasChanged();
    }

    protected async Task HandleMaterialApprovalUpdate(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        ViewModel.HasMaterialApprovalErrors = (materialApproval != null && materialApproval.HazardousNonhazardous == Contracts.Lookups.HazardousClassification.Undefined) ? true : false;
        if (_quantitiesComponent != null)
        {
            await _quantitiesComponent.HandleWeightChange(true); //weights apply differently to separate MAs
        }
        ViewModel.TriggerStateChanged();
    }

    protected async Task HandleMaterialApprovalAcknowledgementDecline()
    {
        await ViewModel.SetMaterialApproval(null);
        StateHasChanged();
    }

    protected void HandleFacilityServiceLoading(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(FacilityServiceSubstanceIndex.FacilityId)] = ViewModel.Facility?.Id ?? ViewModel.TruckTicket.FacilityId;
        criteria.Filters[nameof(FacilityServiceSubstanceIndex.IsAuthorized)] = true;
        criteria.AddFilterIf(ViewModel.TruckTicket.EnforceSpartanFacilityServiceLock is true, nameof(FacilityServiceSubstanceIndex.FacilityServiceId),
                             ViewModel.TruckTicket.LockedSpartanFacilityServiceId);
    }

    protected void HandleLoadDateChange(DateTimeOffset? value)
    {
        ViewModel.SetLoadDate(value);
        SetDefaultDensities();
    }

    protected async Task OpenConversionCalculator()
    {
        await DialogService.OpenAsync<DensityConversionCalculator>("Conversion Calculator", new()
        {
            { nameof(DensityConversionCalculator.TruckTicket), ViewModel.TruckTicket },
            { nameof(DensityConversionCalculator.DefaultDensityFactorsByWeight), _defaultDensityFactorsByWeight },
            { nameof(DensityConversionCalculator.DefaultDensityFactorsByMidWeight), _defaultDensityFactorsByMidWeight },
            { nameof(DensityConversionCalculator.OnCancel), HandleConversionCalculatorCancel },
            { nameof(DensityConversionCalculator.OnCutsUpdate), HandleConversionCalculatorCutUpdate },
        }, new()
        {
            Width = "65%",
            CloseDialogOnOverlayClick = false,
        });
    }

    protected void SetTimeInToCurrentTime()
    {
        ViewModel.TruckTicket.TimeIn = DateTimeOffset.Now;
    }

    protected void SetFacilityFilters(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Facility.Type)] = new AxiomModel
        {
            Value = FacilityType.Lf.ToString(),
            Field = nameof(Facility.Type),
            Key = $"{nameof(Facility.Type)}1",
            Operator = ViewModel.TruckTicket.TruckTicketType == TruckTicketType.LF ? CompareOperators.eq : CompareOperators.ne,
        };
    }

    private async Task HandleFacilityChange(Facility facility)
    {
        await ViewModel.SetFacility(facility);
        HandleFacilityLoad(facility);
        await _facilityServiceDropDown.Reload();
        await _sourceLocationDropDown.Reload();
    }

    private void HandleFacilityLoad(Facility facility)
    {
        ViewModel.Facility = facility;
        SetDefaultDensities();
    }

    private void HandleSourceLocationLoading(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(SourceLocation.CountryCode)] = ViewModel.TruckTicket.CountryCode.ToString();
    }

    private async Task SourceLocationRefreshSearchCache()
    {
        ViewModel.IsSourceLocationDropDownCacheRefresh = true;
        await _sourceLocationDropDown.RefreshCache(_sourceLocationSearchCriteriaModel);
    }

    private async Task FacilityServiceRefreshSearchCache()
    {
        ViewModel.IsFacilityServiceDropDownCacheRefresh = true;
        await _facilityServiceDropDown.RefreshCache(_facilityServiceSearchCriteriaModel);
    }

    private async Task SpartanProductParameterRefreshSearchCache()
    {
        ViewModel.IsSpartanProductParameterDropDownCacheRefresh = true;
        await _spartanProductParameterDropDown.RefreshCache(_spartanProductParameterSearchCriteriaModel);
    }

    private async Task TruckingCompanyRefreshSearchCache()
    {
        ViewModel.IsTruckingCompanyDropDownCacheRefresh = true;
        await _truckingCompanyDropDown.RefreshCache(_truckingCompanySearchCriteriaModel);
    }

    private void OnTruckingCompanyLoading(SearchCriteriaModel criteria)
    {
        //Apply filter on TruckingCompany by Facility LegalEntity
        if (ViewModel.Facility == null || ViewModel.Facility.LegalEntityId == Guid.Empty)
        {
            return;
        }

        criteria.Filters[nameof(Account.LegalEntityId)] = ViewModel.Facility.LegalEntityId;
    }

    private void HandleTruckingCompanyLoad(Account truckingCompany)
    {
        ViewModel.TruckingCompany = truckingCompany;
    }

    private async Task OnSourceLocationChange(SourceLocation selectedSourceLocation)
    {
        await ViewModel.SetSourceLocation(selectedSourceLocation);
        SetDefaultDensities();
    }

    private void HandleSourceLocationLoad(SourceLocation loadedSourceLocation)
    {
        ViewModel.LoadSourceLocation(loadedSourceLocation);
        SetDefaultDensities();
    }

    private async Task OnFacilityServiceChange(FacilityServiceSubstanceIndex facilityServiceSubstanceIndex)
    {
        await ViewModel.SetFacilityService(facilityServiceSubstanceIndex);
        SetDefaultDensities();
    }

    private void HandleFacilityServiceLoad(FacilityServiceSubstanceIndex facilityServiceSubstanceIndex)
    {
        ViewModel.FacilityServiceSubstanceIndex = facilityServiceSubstanceIndex;
    }

    private void SetDefaultDensities()
    {
        if (ViewModel.Facility is null)
        {
            return;
        }

        _defaultDensityFactorsByWeight = FilterDefaultDensities(ViewModel.Facility.WeightConversionParameters);
        _defaultDensityFactorsByMidWeight = FilterDefaultDensities(ViewModel.Facility.MidWeightConversionParameters);
    }

    private PreSetDensityConversionParams FilterDefaultDensities(List<PreSetDensityConversionParams> defaultDensities)
    {
        var truckTicket = ViewModel.TruckTicket;
        var loadDate = truckTicket.LoadDate?.Date ?? DateTimeOffset.MinValue.Date;
        var sourceLocationId = truckTicket.SourceLocationId;
        var facilityServiceId = truckTicket.FacilityServiceId ?? Guid.Empty;

        int ComputeDensityConversionParamRank(PreSetDensityConversionParams defaultParam)
        {
            if (defaultParam.FacilityServiceId.Any())
            {
                return 2;
            }

            return defaultParam.SourceLocationId is not null ? 1 : 0;
        }

        var selectedDefault = defaultDensities
                             .Where(defaultsParams => defaultsParams.IsEnabled)
                             .Where(defaultParams => loadDate >= defaultParams.StartDate.Date && (defaultParams.EndDate is null || defaultParams.EndDate.Value.Date >= loadDate))
                             .Where(defaultParams => defaultParams.FacilityServiceId is null || defaultParams.FacilityServiceId.Count == 0 ||
                                                     defaultParams.FacilityServiceId.Contains(facilityServiceId))
                             .Where(defaultParams => defaultParams.SourceLocationId is null || defaultParams.SourceLocationId == sourceLocationId)
                             .MaxBy(ComputeDensityConversionParamRank);

        return selectedDefault ?? new PreSetDensityConversionParams
        {
            OilConversionFactor = 1,
            SolidsConversionFactor = 1,
            WaterConversionFactor = 1,
        };
    }

    private async Task OnCreateNewTruckingAccount()
    {
        await _truckingCompanyDropDown.CreateNewAccount();
    }
    private async Task ViewOrUpdateSourceLocation(bool isEdit = false)
    {
        Guid? SourceLocationId = isEdit ? ViewModel.TruckTicket.SourceLocationId : null;
        await _sourceLocationDropDown.CreateOrUpdateSourceLocation(ViewModel.TruckTicket.CountryCode, SourceLocationId);
        await SourceLocationRefreshSearchCache();
    }
    private async Task ViewOrUpdateMaterialApproval(bool isEdit = false)
    {
        Guid? MaterialApprovalId = isEdit ? ViewModel.TruckTicket.MaterialApprovalId : null;

        await DialogService.OpenAsync<MaterialApprovalEditPage>(isEdit ? $"Edit - Material Approval" : $"New - Material Approval", new()
        {
            { nameof(MaterialApprovalEditPage.MaterialApproval), ViewModel.CreateNewMaterialApproval()},
            { nameof(MaterialApprovalEditPage.IsEditable), true},
            { nameof(MaterialApprovalEditPage.MaterialApprovalId), MaterialApprovalId },
            { "CreateOrUpdateMaterialApproval", new EventCallback<Contracts.Models.Operations.MaterialApproval>(this, HandleAddEditMaterialApproval) },
        }, new()
        {
            Width = "80%",
            Height = "90%",
        });
    }
    protected async Task HandleAddEditMaterialApproval(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        await SourceLocationRefreshSearchCache();
        var materialApprovalsList = ViewModel.MaterialApprovals.ToList();
        if (materialApprovalsList.Any(x => x.Id == materialApproval.Id))
        {
            int i = materialApprovalsList.FindIndex(x => x.Id == materialApproval.Id);
            materialApprovalsList[i] = materialApproval;
        }
        else
        {
            materialApprovalsList.Add(materialApproval);
        }
        ViewModel.MaterialApprovals = materialApprovalsList;
        ViewModel.TruckTicket.MaterialApprovalId = materialApproval.Id;
        StateHasChanged();
        await HandleMaterialApprovalChange();
    }

    private async Task HandleAnalyticalExpiration(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        if (materialApproval?.AnalyticalExpiryDate != null && materialApproval.AnalyticalExpiryAlertActive)
        {
            var daysToExpiration = materialApproval.AnalyticalExpiryDate.Value.Subtract(DateTimeOffset.UtcNow).Days;

            if (daysToExpiration < 31)
            {
                await ShowMessage("Analytical Expiry Alert",
                                  "<p>The analytical for this Material Approval is nearing expiration or has expired. The analytical needs to be renewed, " +
                                  "as the Material Approval will not be available for use once the analytical expires. If the job is no longer active, " +
                                  "notifications will need to be turned off on the Material Approval.</p>" +
                                  "<br>" +
                                  $"<p>Material Approval ID: {materialApproval.MaterialApprovalNumber}</p>" +
                                  $"<p>Analytical Expiry Date: {materialApproval.AnalyticalExpiryDate.Value:MM/dd/yyyy}</p>" +
                                  $"<p>Source Location: {materialApproval.SourceLocationFormattedIdentifier}</p>" +
                                  $"<p>Generator Name: {materialApproval.GeneratorName}</p>");
            }

            if (daysToExpiration < 0)
            {
                ViewModel.TruckTicket.MaterialApprovalId = default;
                StateHasChanged();
            }
        }
    }

    private async Task RemoveSpartanFacilityServiceLock()
    {
        var truckTicket = ViewModel.TruckTicket;
        truckTicket.EnforceSpartanFacilityServiceLock = default;

        await _facilityServiceDropDown.Reload();
    }

    private static IReadOnlyDictionary<DowNonDow, string> GetDowNonDowDropDownOptions()
    {
        /*do not want haz and non haz here. */
        var dict = new Dictionary<DowNonDow, string>
        {
            { DowNonDow.Dow, DowNonDow.Dow.GetDescription() },
            { DowNonDow.NonDow, DowNonDow.NonDow.GetDescription() },
        };

        return dict;

    }
}
