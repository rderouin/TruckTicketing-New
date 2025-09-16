using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;
using Trident.Extensions;
using Trident.UI.Blazor.Logging.AppInsights;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public interface ITruckTicketWorkflow
{
    ValueTask Initialize(TruckTicketExperienceViewModel viewModel);

    ValueTask<bool> ShouldRun(TruckTicketExperienceViewModel viewModel);

    ValueTask<bool> Run(TruckTicketExperienceViewModel viewModel);
}

public class TruckTicketExperienceViewModel
{
    private readonly List<BillingConfiguration> _billingConfigurations = new();

    private readonly IServiceProxyBase<Facility, Guid> _facilityService;

    private readonly Dictionary<Guid, FacilityServiceSubstanceIndex> _facilityServiceSubstanceIndexCache = new();

    private readonly IFacilityServiceSubstanceIndexService _facilityServiceSubstanceIndexService;

    private readonly List<Contracts.Models.Operations.MaterialApproval> _materialApprovals = new();

    private readonly ISalesLineService _salesLineService;

    private readonly IServiceProxyBase<ServiceType, Guid> _serviceType;

    private readonly IServiceProxyBase<TruckTicket, Guid> _truckTicketProxyService;

    private readonly TruckTicketWorkflowManager _workflowManager;

    private List<SalesLine> _additionalServiceSalesLines = new();

    private string _billingContactCustomerAddress;

    private bool _isRemovingSalesLines;

    private List<SalesLine> _primarySalesLines = new();

    private List<SalesLine> _reversedSalesLines = new();

    private List<SalesLine> _userAddedAdditionalServiceSalesLines = new();

    public IEnumerable<string> ActiveTicketNumbers = Array.Empty<string>();

    public string BillingCustomerAddress;

    public bool HasMaterialApprovalErrors;

    public bool IsFacilityServiceDropDownCacheRefresh;

    public bool IsRefresh;

    public bool IsRefreshMaterialApproval;

    public bool IsSettingBillingConfiguration;

    public bool IsSourceLocationDropDownCacheRefresh;

    public bool IsSpartanProductParameterDropDownCacheRefresh;

    public bool IsTruckingCompanyDropDownCacheRefresh;
    //volume cut validation

    public Dictionary<string, string> VolumeCutValidationErrors = new();

    public TruckTicketExperienceViewModel(IServiceProxyBase<Facility, Guid> facilityService,
                                          IServiceProxyBase<ServiceType, Guid> serviceType,
                                          IServiceProxyBase<TruckTicket, Guid> truckTicketProxyService,
                                          TruckTicketWorkflowManager workflowManager,
                                          IFacilityServiceSubstanceIndexService facilityServiceSubstanceIndexService,
                                          IApplicationInsights appInsights,
                                          ISalesLineService salesLineService)
    {
        _facilityService = facilityService;
        _serviceType = serviceType;
        _truckTicketProxyService = truckTicketProxyService;
        _workflowManager = workflowManager;
        _facilityServiceSubstanceIndexService = facilityServiceSubstanceIndexService;
        _appInsights = appInsights;
        _salesLineService = salesLineService;
    }

    private IApplicationInsights _appInsights { get; }

    public ITruckTicketWorkflow[] Workflows { get; set; }

    public ICollection<Contracts.Models.Operations.MaterialApproval> MaterialApprovals { get; set; } = new List<Contracts.Models.Operations.MaterialApproval>();

    public ICollection<BillingConfiguration> BillingConfigurations { get; set; } = new List<BillingConfiguration>();

    public bool DisableConversionCalculator =>
        TruckTicket.FacilityId == Guid.Empty || TruckTicket.SourceLocationId == Guid.Empty || TruckTicket.FacilityServiceId == null ||
        TruckTicket.FacilityServiceId == Guid.Empty || TruckTicket.LoadDate == null || TruckTicket.LoadDate == DateTimeOffset.MinValue;

    public Guid BillingCustomerId => TruckTicket.BillingCustomerId;

    public TruckTicketStatus TruckTicketStatus
    {
        get => TruckTicket.Status;
        set
        {
            TruckTicket.Status = value;
            TriggerStateChanged();
        }
    }

    public string VoidReason
    {
        get => TruckTicket.VoidReason;
        set
        {
            TruckTicket.VoidReason = value;
            TriggerStateChanged();
        }
    }

    public string HoldReason
    {
        get => TruckTicket.HoldReason;
        set
        {
            TruckTicket.HoldReason = value;
            TriggerStateChanged();
        }
    }

    public string OtherReason
    {
        get => TruckTicket.OtherReason;
        set
        {
            TruckTicket.OtherReason = value;
            TriggerStateChanged();
        }
    }

    public string TrailerNumber
    {
        get => TruckTicket.TrailerNumber;
        set
        {
            TruckTicket.TrailerNumber = value.ToUpper();
            TriggerStateChanged();
        }
    }

    public string TruckNumber
    {
        get => TruckTicket.TruckNumber;
        set
        {
            TruckTicket.TruckNumber = value.ToUpper();
            TriggerStateChanged();
        }
    }

    public bool LandfillSampled
    {
        get => TruckTicket.LandfillSampled;
        set
        {
            TruckTicket.LandfillSampled = value;
            TriggerStateChanged();
        }
    }

    public bool IsScaleTicketView =>
        TruckTicket.TruckTicketType == TruckTicketType.LF ||
        Facility?.Type == FacilityType.Lf;

    public bool IsWorkTicketView =>
        TruckTicket.TruckTicketType == TruckTicketType.WT ||
        Facility?.Type == FacilityType.Fst;

    public bool IsServiceOnlyTicket => TruckTicket?.IsServiceOnlyTicket == true;

    public Response<TruckTicketSalesPersistenceResponse> UpsertResponse { get; private set; }

    public TruckTicket TruckTicket { get; set; }

    public TruckTicket TruckTicketBackup { get; set; }

    public Facility Facility { get; set; }

    public FacilityServiceSubstanceIndex FacilityServiceSubstanceIndex { get; set; }

    public Account TruckingCompany { get; set; }

    public SourceLocation SourceLocation { get; protected set; }

    public ServiceType ServiceType { get; set; }

    public Account BillingCustomer { get; protected set; }

    public IReadOnlyCollection<SalesLine> SalesLines => _primarySalesLines.Concat(_additionalServiceSalesLines).Concat(_userAddedAdditionalServiceSalesLines).ToList();

    public IReadOnlyCollection<SalesLine> ReversedSalesLines => _reversedSalesLines.ToList();

    public IReadOnlyCollection<SalesLine> CombinedAdditionalServiceSalesLines => _additionalServiceSalesLines.Concat(_userAddedAdditionalServiceSalesLines).ToList();

    public bool ShowMaterialApproval =>
        Facility?.Type == FacilityType.Lf ||
        (Facility?.ShowMaterialApproval ?? false) ||
        HasMaterialApprovalValue;

    public bool HasMaterialApprovalValue => TruckTicket?.MaterialApprovalId.GetValueOrDefault(Guid.Empty) != Guid.Empty;

    public bool IsRunningWorkflows { get; private set; }

    public bool IsLoadingBillingConfigurations { get; set; }

    public bool IsLoadingSalesLines { get; set; }

    public bool IsRemovingSalesLines
    {
        get => _isRemovingSalesLines;
        set
        {
            _isRemovingSalesLines = value;
            TriggerStateChanged();
        }
    }

    public bool ActivateAutofillTareWeight { get; private set; }

    public bool IsInitializing { get; set; }

    public bool IsInvoiceReachedThreshold { get; set; }

    public string InvoiceThresholdViolationMessage { get; set; }

    public bool IsRefreshDisabled => TruckTicket?.Status is not TruckTicketStatus.New and not TruckTicketStatus.Hold and not TruckTicketStatus.Open;

    [Inject]
    private TruckTicketWorkflowManager WorkflowManager { get; set; }

    public bool IsTotalVolumePercentInvalid => TruckTicket is not null && !IsServiceOnlyTicket && Math.Abs(TruckTicket.TotalVolumePercent - 100) > 0.011;

    public bool IsTotalVolumeInvalid =>
        TruckTicket is not null && !IsServiceOnlyTicket && (TruckTicket.LoadVolume is null || Math.Abs(TruckTicket.TotalVolume - (TruckTicket.LoadVolume ?? 0)) > 0.005);

    public bool HasActiveDuplicateSalesLines()
    {
        return SalesLines
              .Where(salesLine => !salesLine.IsReversal && !salesLine.IsReversed)
              .GroupBy(salesLine => salesLine.ProductNumber)
              .Any(lines => lines.Count() > 1);
    }

    public bool HasStaleSalesLines()
    {
        if (TruckTicket.IsServiceOnlyTicket == true)
        {
            return false;
        }

        var loadQuantities = new[] { TruckTicket.OilVolume, TruckTicket.WaterVolume, TruckTicket.SolidVolume, TruckTicket.TotalVolume, TruckTicket.NetWeight }
                            .Where(value => value >= 0.009).ToList();

        if (!loadQuantities.Any())
        {
            return false;
        }

        var salesLineQuantities = _primarySalesLines.Select(salesLine => salesLine.Quantity).ToList();

        // Added condition to allow save ticket if TareWeight is zero and sales line not generated #11958
        if (TruckTicket.NetWeight > 0 && TruckTicket.TareWeight == 0 && SalesLines.Count == 0)
        {
            return false;
        }

        // Check if there is any non-zero load quantities that dont have corresponding sales lines
        return loadQuantities.All(loadQuantity => !salesLineQuantities.Any(salesLineQuantity => Math.Abs(loadQuantity - salesLineQuantity) <= 0.001));
    }

    public bool HasZeroAmountInAdditionalServices()
    {
        return CombinedAdditionalServiceSalesLines.Any(sl => Math.Abs(sl.Quantity) < 0.001);
    }

    public async Task TriggerWorkflows()
    {
        IsRunningWorkflows = true;
        TriggerStateChanged();

        await _workflowManager.TriggerWorkflows(this);

        IsRunningWorkflows = false;
        TriggerStateChanged();
    }

    public event Func<PropertyChangedEventArgs, Task> PropertyChanged;

    public event Func<Task> StateChanged;

    public event Func<Task> Initialized;

    public event Func<Task> TruckTicketContainerRefresh;

    public event Func<Task> RefreshTruckTicket;

    public event Func<Task> TicketSaved;

    public async Task Initialize(TruckTicket truckTicket = null)
    {
        await _appInsights.StartTrackEvent("truckticket-initialize");
        IsInitializing = true;
        TriggerStateChanged();

        Facility = null;
        UpsertResponse = null;
        BillingCustomer = null;
        SourceLocation = null;
        ServiceType = null;
        _billingContactCustomerAddress = null;
        FacilityServiceSubstanceIndex = null;
        BillingConfigurations = new List<BillingConfiguration>();
        _materialApprovals.Clear();
        _billingConfigurations.Clear();
        //Clear unsaved user added additional services SalesLines when changing ticket; but persist when ticket refreshed
        if (!IsRefresh)
        {
            _userAddedAdditionalServiceSalesLines = new();
        }

        _reversedSalesLines.Clear();
        _additionalServiceSalesLines.Clear();
        _primarySalesLines.Clear();
        VolumeCutValidationErrors.Clear();
        InvoiceThresholdViolationMessage = string.Empty;

        if (truckTicket != null)
        {
            TruckTicket = truckTicket.Clone();
            TruckTicketContainerRefresh?.Invoke();
            TruckTicketBackup = TruckTicket.Clone();
            TruckTicket.PropertyChanged += TruckTicketOnPropertyChanged;
        }

        await Task.WhenAll(SetSalesLines(new List<SalesLine>()), UpdateServiceType(!IsRefresh), UpdateFacility(!IsRefresh));

        await _workflowManager.InitializeWorkflows(this);

        IsInitializing = false;
        await _appInsights.StopTrackEvent("truckticket-initialize", new()
        {
            [nameof(truckTicket.TicketNumber)] = truckTicket?.TicketNumber,
        });

        Initialized?.Invoke();
        if (IsRefresh)
        {
            RefreshTruckTicket?.Invoke();
        }
    }

    private async Task UpdateFacility(bool useCache = true)
    {
        if (TruckTicket != null && TruckTicket.TicketNumber.HasText() && Facility?.Id != TruckTicket?.FacilityId)
        {
            Facility = await _facilityService.GetById(TruckTicket!.FacilityId, useCache);
        }
    }

    public void SetReversedSalesLines(List<SalesLine> salesLines)
    {
        _reversedSalesLines = salesLines;
        TriggerStateChanged();
    }

    public void TriggerAfterSave()
    {
        TruckTicketBackup = TruckTicket.Clone();
        TruckTicket.PropertyChanged += TruckTicketOnPropertyChanged;
        TicketSaved?.Invoke();
    }

    public async Task ReloadCurrentTruckTicket()
    {
        var updatedTicket = await _truckTicketProxyService.GetById(TruckTicket.Id);
        TruckTicket = updatedTicket;
        TruckTicketBackup = updatedTicket.Clone();
        SetActiveTicketNumbers(new[] { updatedTicket.TicketNumber });
    }

    private async Task UpdateServiceType(bool useCache = true)
    {
        if (TruckTicket != null && TruckTicket.ServiceTypeId != null && ServiceType?.Id != TruckTicket.ServiceTypeId)
        {
            ServiceType = await _serviceType.GetById(TruckTicket.ServiceTypeId.Value, useCache);
            TruckTicket.ServiceTypeClass = ServiceType.Class;
            TriggerStateChanged();
        }
    }

    private void TruckTicketOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
#pragma warning disable CS4014
        TriggerWorkflows();
#pragma warning restore CS4014
        PropertyChanged?.Invoke(e);
    }

    public void SetActiveTicketNumbers(IEnumerable<string> activeTicketNumbers)
    {
        ActiveTicketNumbers = activeTicketNumbers;
        TriggerStateChanged();
    }

    public void SetTruckTicketStatus(TruckTicketStatus truckTicketStatus)
    {
        TruckTicket.Status = truckTicketStatus;
        TriggerStateChanged();
    }

    public void SetTruckTicket(TruckTicket truckTicket)
    {
        TruckTicket = truckTicket;
        TriggerStateChanged();
    }

    public async Task SetSourceLocation(SourceLocation sourceLocation)
    {
        SourceLocation = sourceLocation;
        TruckTicket.SourceLocationUnformatted = sourceLocation?.Identifier;
        TruckTicket.SourceLocationName = sourceLocation?.SourceLocationName;
        TruckTicket.SourceLocationFormatted = sourceLocation?.FormattedIdentifier;
        TruckTicket.SourceLocationCode = sourceLocation?.SourceLocationCode;
        TruckTicket.SourceLocationId = sourceLocation?.Id ?? Guid.Empty;
        LoadGeneratorBasedOnLoadDate();

        await TriggerWorkflows();
        TriggerStateChanged();
    }

    public void LoadSourceLocation(SourceLocation sourceLocation)
    {
        SourceLocation = sourceLocation;
    }

    public void SetLoadDate(DateTimeOffset? value)
    {
        TruckTicket.LoadDate = value;
        if (TruckTicket.LoadDate.HasValue)
        {
            var loadDate = TruckTicket.LoadDate.Value;
            TruckTicket.LoadDate = new DateTimeOffset(loadDate.Year, loadDate.Month, loadDate.Day, 7, 0, 1, new(0)).ToAlbertaOffset();
        }

        LoadGeneratorBasedOnLoadDate();
    }

    private void LoadGeneratorBasedOnLoadDate()
    {
        if (TruckTicket.SourceLocationId == Guid.Empty)
        {
            return;
        }

        var ownershipHistory = SourceLocation?.OwnershipHistory?.OrderByDescending(x => x.EndDate).ToList();
        if (TruckTicket.LoadDate == null || ownershipHistory == null || !ownershipHistory.Any() || ownershipHistory.First().EndDate < TruckTicket.LoadDate)
        {
            TruckTicket.GeneratorId = SourceLocation?.GeneratorId ?? Guid.Empty;
            TruckTicket.GeneratorName = SourceLocation?.GeneratorName;
        }
        else
        {
            var ownershipRecordForLoadDate = ownershipHistory.FirstOrDefault(x => x.StartDate < TruckTicket.LoadDate && x.EndDate > TruckTicket.LoadDate, new());
            TruckTicket.GeneratorId = ownershipRecordForLoadDate?.GeneratorId ?? Guid.Empty;
            TruckTicket.GeneratorName = ownershipRecordForLoadDate?.GeneratorName;
        }
    }

    public async Task SetFacility(Facility facility)
    {
        Facility = facility;
        TruckTicket.FacilityId = facility.Id;
        TruckTicket.FacilityName = facility.Name;
        TruckTicket.FacilityType = facility.Type;
        TruckTicket.FacilityLocationCode = facility.LocationCode;
        TruckTicket.SiteId = facility.SiteId;
        TruckTicket.CountryCode = facility.CountryCode;
        TruckTicket.LegalEntity = facility.LegalEntity;
        TruckTicket.LegalEntityId = facility.LegalEntityId;

        if (!TruckTicketBackup.TicketNumber.HasText() && TruckTicketBackup.FacilityId != facility.Id)
        {
            TruckTicketBackup.FacilityId = facility.Id;
            TruckTicket.FacilityServiceSubstanceId = default;
            TruckTicket.SubstanceId = Guid.Empty;
            TruckTicket.SubstanceName = default;
            TruckTicket.WasteCode = default;
            TruckTicket.ServiceTypeId = default;
            ServiceType = default;
            TruckTicket.ServiceType = default;
            TruckTicket.FacilityServiceId = default;
        }

        if (TruckTicket.DowNonDow == DowNonDow.Undefined)
        {
            TruckTicket.DowNonDow = facility.DowNonDow;
        }

        await TriggerWorkflows();
        TriggerStateChanged();
    }

    public void SetBillingCustomer(Account account)
    {
        BillingCustomer = account ?? new Account();
        TruckTicket.BillingCustomerName = account?.Name;
        TruckTicket.BillingCustomerId = account?.Id ?? Guid.Empty;
        SetBillingCustomerAddress();
        TriggerStateChanged();
    }

    public async Task SetMaterialApproval(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        TruckTicket.MaterialApprovalId = materialApproval?.Id;
        TruckTicket.MaterialApprovalNumber = materialApproval?.MaterialApprovalNumber;
        TruckTicket.Acknowledgement = materialApproval == null || !materialApproval.ScaleOperatorNotes.HasText() ? string.Empty : TruckTicket.Acknowledgement;
        TruckTicket.FacilityServiceSubstanceId = materialApproval?.FacilityServiceSubstanceIndexId;
        TruckTicket.FacilityServiceId = materialApproval?.FacilityServiceId;
        TruckTicket.SubstanceId = materialApproval?.SubstanceId ?? Guid.Empty;
        TruckTicket.SubstanceName = materialApproval?.SubstanceName;
        TruckTicket.WasteCode = materialApproval?.WasteCodeName;
        TruckTicket.ServiceType = materialApproval?.FacilityServiceName;
        TruckTicket.ServiceTypeId = materialApproval?.ServiceTypeId;
        Enum.TryParse<Stream>(materialApproval?.Stream, out var stream);
        TruckTicket.Stream = stream;
        TruckTicket.FacilityStreamRegulatoryCode = stream switch
                                                   {
                                                       Stream.Water => Facility?.Water,
                                                       Stream.Waste => Facility?.Waste,
                                                       Stream.Terminalling => Facility?.Terminaling,
                                                       Stream.Pipeline => Facility?.Pipeline,
                                                       Stream.Treating => Facility?.Treating,
                                                       _ => "",
                                                   };

        ActivateAutofillTareWeight = materialApproval?.ActivateAutofillTareWeight ?? default;
        TruckTicket.WellClassification = TruckTicket.TruckTicketType == TruckTicketType.LF
                                             ? materialApproval?.WellClassification ?? default
                                             : materialApproval?.WellClassification ?? TruckTicket.WellClassification;

        //Check if facilityService is ServiceOnly
        FacilityServiceSubstanceIndex facilityServiceSubstanceIndex = null;
        if (TruckTicket.FacilityServiceSubstanceId.HasValue &&
            (!_facilityServiceSubstanceIndexCache.TryGetValue(TruckTicket.FacilityServiceSubstanceId.Value, out facilityServiceSubstanceIndex) ||
             facilityServiceSubstanceIndex == null))
        {
            facilityServiceSubstanceIndex = await FetchFacilityServiceSubstanceIndex(TruckTicket.FacilityServiceSubstanceId.Value);
            _facilityServiceSubstanceIndexCache[TruckTicket.FacilityServiceSubstanceId.Value] = facilityServiceSubstanceIndex;
        }

        TruckTicket.IsServiceOnlyTicket = facilityServiceSubstanceIndex?.IsServiceOnlyProduct();

        if (TruckTicket.TruckingCompanyId == Guid.Empty || !TruckTicket.TicketNumber.HasText())
        {
            TruckTicket.TruckingCompanyId = materialApproval?.TruckingCompanyId ?? Guid.Empty;
            TruckTicket.TruckingCompanyName = materialApproval?.TruckingCompanyName;
            PropertyChanged?.Invoke(new(nameof(TruckTicket.TruckingCompanyId)));
        }

        //Remove references for BillingConfiguration when no material approval returned
        if (materialApproval == null)
        {
            BillingConfigurations = new List<BillingConfiguration>();
            await SetBillingConfiguration();
        }

        await UpdateServiceType();
        await TriggerWorkflows();
        TriggerStateChanged();
    }

    private async Task<FacilityServiceSubstanceIndex> FetchFacilityServiceSubstanceIndex(Guid? id)
    {
        if (id == null || id == Guid.Empty)
        {
            return null;
        }

        return await _facilityServiceSubstanceIndexService.GetById(id.Value);
    }

    public async Task SetWellClassification(WellClassifications wellClassification)
    {
        TruckTicket.WellClassification = wellClassification;
        await TriggerWorkflows();
        TriggerStateChanged();
    }

    public void CloseActiveTruckTicket()
    {
        if (TruckTicket != null)
        {
            TruckTicket.PropertyChanged -= TruckTicketOnPropertyChanged;
        }

        SetTruckTicket(null);
        TriggerStateChanged();
    }

    public void TriggerStateChanged()
    {
        StateChanged?.Invoke();
    }

    public async Task SetFacilityService(FacilityServiceSubstanceIndex facilityService)
    {
        TruckTicket.FacilityServiceSubstanceId = facilityService.Id;
        TruckTicket.SubstanceId = facilityService.SubstanceId;
        TruckTicket.SubstanceName = facilityService.Substance;
        TruckTicket.WasteCode = facilityService.WasteCode;
        TruckTicket.ServiceTypeId = facilityService.ServiceTypeId;
        TruckTicket.IsServiceOnlyTicket = facilityService.IsServiceOnlyProduct();
        _facilityServiceSubstanceIndexCache[facilityService.Id] = facilityService;
        var previousServiceType = ServiceType;
        await UpdateServiceType();
        ClearVolumeCut(previousServiceType);
        TruckTicket.ServiceType = facilityService.ServiceTypeName;
        TruckTicket.FacilityServiceId = facilityService.FacilityServiceId;
        TruckTicket.UnitOfMeasure = facilityService.UnitOfMeasure;
        Enum.TryParse<Stream>(facilityService.Stream, out var stream);
        TruckTicket.Stream = stream;
        TruckTicket.FacilityStreamRegulatoryCode = stream switch
                                                   {
                                                       Stream.Water => Facility?.Water,
                                                       Stream.Waste => Facility?.Waste,
                                                       Stream.Terminalling => Facility?.Terminaling,
                                                       Stream.Pipeline => Facility?.Pipeline,
                                                       Stream.Treating => Facility?.Treating,
                                                       _ => "",
                                                   };

        await TriggerWorkflows();
        TriggerStateChanged();
    }

    private void ClearVolumeCut(ServiceType previousServiceType)
    {
        var allowCutReset = TruckTicket.TruckTicketType is TruckTicketType.WT ||
                            (TruckTicket.TruckTicketType is TruckTicketType.SP && TruckTicket.Source is TruckTicketSource.Manual);

        if (!allowCutReset || (previousServiceType?.IncludesOil == ServiceType?.IncludesOil && previousServiceType?.IncludesWater == ServiceType?.IncludesWater &&
                               previousServiceType?.IncludesSolids == ServiceType?.IncludesSolids))
        {
            if (TruckTicket.TotalVolume > 0)
            {
                SetLoadVolumeChange();
            }

            return;
        }

        TruckTicket.OilVolume = default;
        TruckTicket.WaterVolume = default;
        TruckTicket.SolidVolume = default;

        TruckTicket.OilVolumePercent = default;
        TruckTicket.WaterVolumePercent = default;
        TruckTicket.SolidVolumePercent = default;

        TruckTicket.TotalVolume = default;
        TruckTicket.TotalVolumePercent = default;
    }

    public void SetLoadVolumeChange()
    {
        if (TruckTicket.CutEntryMethod == CutEntryMethod.FixedValue)
        {
            SetCutVolumeChange();
        }
        else
        {
            SetCutPercentageChange();
        }
    }

    public void SetCutVolumeChange()
    {
        var totalVolume = TruckTicket.OilVolume + TruckTicket.WaterVolume + TruckTicket.SolidVolume;

        TruckTicket.OilVolumePercent = totalVolume > 0 ? Math.Round(TruckTicket.OilVolume * 100.0 / totalVolume, 1) : 0;
        TruckTicket.WaterVolumePercent = totalVolume > 0 ? Math.Round(TruckTicket.WaterVolume * 100.0 / totalVolume, 1) : 0;
        TruckTicket.SolidVolumePercent = totalVolume > 0 ? Math.Round(TruckTicket.SolidVolume * 100.0 / totalVolume, 1) : 0;
        TruckTicket.TotalVolumePercent = TruckTicket.OilVolumePercent + TruckTicket.WaterVolumePercent + TruckTicket.SolidVolumePercent;
        TruckTicket.TotalVolume = totalVolume;
        if (ServiceType is not null && !(ServiceType.IncludesOil || ServiceType.IncludesWater || ServiceType.IncludesSolids))
        {
            TruckTicket.TotalVolume = TruckTicket.LoadVolume ?? 0;
            TruckTicket.TotalVolumePercent = TruckTicket.LoadVolume == 0 ? 0 : 100;
        }

        SetCutLabelValidation();
    }

    public void SetCutPercentageChange()
    {
        TruckTicket.TotalVolumePercent = TruckTicket.OilVolumePercent + TruckTicket.WaterVolumePercent + TruckTicket.SolidVolumePercent;

        TruckTicket.OilVolume = Math.Round((TruckTicket.LoadVolume ?? 0) * (TruckTicket.OilVolumePercent / 100.0), 1);
        TruckTicket.WaterVolume = Math.Round((TruckTicket.LoadVolume ?? 0) * (TruckTicket.WaterVolumePercent / 100.0), 1);
        TruckTicket.SolidVolume = Math.Round((TruckTicket.LoadVolume ?? 0) * (TruckTicket.SolidVolumePercent / 100.0), 1);
        TruckTicket.TotalVolume = TruckTicket.OilVolume + TruckTicket.WaterVolume + TruckTicket.SolidVolume;
        if (ServiceType is not null && !(ServiceType.IncludesOil || ServiceType.IncludesWater || ServiceType.IncludesSolids))
        {
            TruckTicket.TotalVolumePercent = 100.0;
        }

        SetCutLabelValidation();
    }

    public void SetEdiValues(List<EDIFieldValue> ediFieldValues, bool isValid)
    {
        TruckTicket.EdiFieldValues = new(ediFieldValues);
        TruckTicket.IsEdiValid = isValid;
    }

    public async Task SetTruckingCompany(Account account)
    {
        TruckTicket.TruckingCompanyId = account?.Id ?? Guid.Empty;
        TruckTicket.TruckingCompanyName = account?.Name;
        await TriggerWorkflows();
        TriggerStateChanged();
    }

    public async Task RefreshPrice(SalesLine salesLine)
    {
        var priceRequest = new SalesLinePriceRequest
        {
            CustomerId = BillingCustomerId,
            FacilityId = Facility.Id,
            TruckTicketDate = TruckTicket.LoadDate ?? default,
            ProductNumber = salesLine.ProductNumber,
            SourceLocation = salesLine.SourceLocationFormattedIdentifier,
        };

        try
        {
            var price = await _salesLineService.GetPrice(priceRequest);
            salesLine.Rate = price;
        }
        catch (Exception)
        {
            salesLine.Rate = 0;
            salesLine.Status = SalesLineStatus.Exception;
        }

        SetTotalValue(salesLine);
    }

    public void SetTotalValue(SalesLine salesLine)
    {
        salesLine.TotalValue = Math.Round(salesLine.Quantity * salesLine.Rate, 2);
    }

    public async Task SetSalesLines(IEnumerable<SalesLine> salesLines, bool clearUserAddedAdditionalServices = false)
    {
        var enumerable = salesLines?.ToList() ?? new();
        var primarySalesLines = enumerable.Where(s => !s.IsAdditionalService).ToList();
        var additionalServiceSalesLines = enumerable.Where(s => s.IsAdditionalService).ToList();

        _primarySalesLines = primarySalesLines;

        if (clearUserAddedAdditionalServices)
        {
            _userAddedAdditionalServiceSalesLines = new();
        }

        if (_userAddedAdditionalServiceSalesLines.Any())
        {
            foreach (var persistedLines in _userAddedAdditionalServiceSalesLines)
            {
                await RefreshPrice(persistedLines);
            }
        }

        //While refreshing ticket, Initialize is clearing _additionalServiceSalesLines, which is what is used to capture user added additional services.
        if (additionalServiceSalesLines.Count > 0 || _additionalServiceSalesLines.Any(s => s.IsReadOnlyLine))
        {
            _additionalServiceSalesLines = additionalServiceSalesLines.ToList();
        }

        TruckTicket.SalesTotalValue = enumerable.Where(salesLine => !salesLine.IsReversed && !salesLine.IsReversal).Sum(salesLine => salesLine.TotalValue);

        TriggerStateChanged();
        await Task.CompletedTask;
    }

    public void RemoveAdditionalServiceSalesLine(SalesLine salesLine)
    {
        _additionalServiceSalesLines.Remove(salesLine);
        _userAddedAdditionalServiceSalesLines.Remove(salesLine);
        TriggerStateChanged();
    }

    public async Task AddAdditionalServiceSalesLine(string productName = null, string productNumber = null, string unitOfMeasure = null, double quantity = default, bool isLoadProduct = false)
    {
        var salesLine = new SalesLine
        {
            Id = Guid.NewGuid(),
            IsAdditionalService = true,
            IsCutLine = false,
            CustomerId = TruckTicket.BillingCustomerId,
            TruckTicketDate = TruckTicket.LoadDate ?? default,
            Status = SalesLineStatus.Preview,
            IsUserAddedAdditionalServices = true,
            CanPriceBeRefreshed = true,
        };

        if (isLoadProduct)
        {
            salesLine.Quantity = quantity;
            await AdditionalServiceProductSelectHandler(productName, productNumber, unitOfMeasure, salesLine);
        }

        _userAddedAdditionalServiceSalesLines.Add(salesLine);

        TriggerStateChanged();
    }

    public async Task AdditionalServiceProductSelectHandler(string productName, string productNumber, string unitOfMeasure, SalesLine salesLine)
    {
        salesLine.ProductName = productName;
        salesLine.ProductNumber = productNumber;
        salesLine.UnitOfMeasure = unitOfMeasure;
        await RefreshPrice(salesLine);
    }

    public async Task SetBillingConfiguration(BillingConfiguration billingConfig = null)
    {
        IsSettingBillingConfiguration = true;
        TruckTicket.IsBillingInfoOverridden = false;

        TruckTicket.BillingConfigurationId = billingConfig?.Id;
        TruckTicket.BillingCustomerId = billingConfig?.BillingCustomerAccountId ?? Guid.Empty;
        TruckTicket.BillingCustomerName = billingConfig?.BillingCustomerName;
        TruckTicket.BillingConfigurationName = billingConfig?.Name;
        TruckTicket.LoadConfirmationFrequency = billingConfig?.LoadConfirmationFrequency;

        if (TruckTicket.BillingContact == null || TruckTicket.BillingContact.AccountContactId != billingConfig?.BillingContactId)
        {
            TruckTicket.BillingContact = new() { AccountContactId = billingConfig?.BillingContactId };
        }

        TruckTicket.EdiFieldValues = billingConfig?
                                    .EDIValueData
                                    .Select(e => e.Clone())
                                    .ToList() ?? new();

        TruckTicket.Signatories = billingConfig?
                                 .Signatories
                                 .Where(e => e.IsAuthorized)
                                 .Select(e => new Signatory
                                  {
                                      AccountContactId = e.AccountContactId,
                                      ContactEmail = e.Email,
                                      ContactPhoneNumber = e.PhoneNumber,
                                      ContactAddress = e.Address,
                                      ContactName = e.FirstName + " " + e.LastName,
                                  })
                                 .ToList() ?? new();

        TruckTicket.IsEdiValid = true;
        IsSettingBillingConfiguration = false;
        await TriggerWorkflows();
        TriggerStateChanged();
    }

    private void SetBillingCustomerAddress()
    {
        if (TruckTicket.BillingContact.AccountContactId != null && TruckTicket.BillingContact.AccountContactId != Guid.Empty)
        {
            //Billing Contact pulled from Billing Configuration - use billing contact address
            BillingCustomerAddress = _billingContactCustomerAddress ?? TruckTicket.BillingContact.Address;
        }
        else
        {
            //No billing contact on Billing Configuration - use customer default address
            BillingCustomerAddress = BillingCustomer?.AccountAddresses?
                                        .FirstOrDefault(a => a.IsPrimaryAddress)?
                                        .Display ?? string.Empty;
        }
    }

    public void SetBillingContact(AccountContactIndex accountContact)
    {
        TruckTicket.BillingContact = new()
        {
            // TODO: Update w/ changes to AccountContactIndex
            // Address = accountContact.AddressDisplay,
            Email = accountContact.Email,
            Name = accountContact.Name,
            PhoneNumber = accountContact.PhoneNumber,
            AccountContactId = accountContact.Id,
        };

        _billingContactCustomerAddress = accountContact.Address;
        SetBillingCustomerAddress();
        TriggerStateChanged();
    }

    public void SetResponse(Response<TruckTicketSalesPersistenceResponse> response)
    {
        UpsertResponse = response;
        TriggerStateChanged();
    }

    public void SetCutLabelValidation()
    {
        VolumeCutValidationErrors.Clear();
        if (TruckTicket.TruckTicketType is not (TruckTicketType.SP or TruckTicketType.WT) || ServiceType is null)
        {
            return;
        }

        //Oil
        if (ServiceType.IncludesOil && ServiceType.OilThresholdType == SubstanceThresholdType.Fixed)
        {
            VolumeCutValidationErrors.Remove(nameof(TruckTicket.OilVolume));
            var oilFixedValidationMessage = $"Oil quantity is outside the range allowed for the selected Service Type {ServiceType.ServiceTypeId}";

            if (ValidateVolumeCutValueRange(TruckTicket.OilVolume, ServiceType.OilMinValue, ServiceType.OilMaxValue))
            {
                VolumeCutValidationErrors.TryAdd(nameof(TruckTicket.OilVolume), oilFixedValidationMessage);
            }
        }
        else if (ServiceType.IncludesOil && ServiceType.OilThresholdType == SubstanceThresholdType.Percentage)
        {
            VolumeCutValidationErrors.Remove(nameof(TruckTicket.OilVolumePercent));
            var oilPercentageValidationMessage = $"Oil percentage is outside the range allowed for the selected Service Type {ServiceType.ServiceTypeId}";
            if (ValidateVolumeCutValueRange(TruckTicket.OilVolumePercent, ServiceType.OilMinValue, ServiceType.OilMaxValue))
            {
                VolumeCutValidationErrors.TryAdd(nameof(TruckTicket.OilVolumePercent), oilPercentageValidationMessage);
            }
        }

        //Water
        if (ServiceType.IncludesWater && ServiceType.WaterThresholdType == SubstanceThresholdType.Fixed)
        {
            VolumeCutValidationErrors.Remove(nameof(TruckTicket.WaterVolume));
            var waterFixedValidationMessage = $"Water quantity is outside the range allowed for the selected Service Type {ServiceType.ServiceTypeId}";
            if (ValidateVolumeCutValueRange(TruckTicket.WaterVolume, ServiceType.WaterMinValue, ServiceType.WaterMaxValue))
            {
                VolumeCutValidationErrors.TryAdd(nameof(TruckTicket.WaterVolume), waterFixedValidationMessage);
            }
        }
        else if (ServiceType.IncludesWater && ServiceType.WaterThresholdType == SubstanceThresholdType.Percentage)
        {
            VolumeCutValidationErrors.Remove(nameof(TruckTicket.WaterVolumePercent));
            var waterPercentageValidationMessage = $"Water percentage is outside the range allowed for the selected Service Type {ServiceType.ServiceTypeId}";

            if (ValidateVolumeCutValueRange(TruckTicket.WaterVolumePercent, ServiceType.WaterMinValue, ServiceType.WaterMaxValue))
            {
                VolumeCutValidationErrors.TryAdd(nameof(TruckTicket.WaterVolumePercent), waterPercentageValidationMessage);
            }
        }

        //Solid
        if (ServiceType.IncludesSolids && ServiceType.SolidThresholdType == SubstanceThresholdType.Fixed)
        {
            VolumeCutValidationErrors.Remove(nameof(TruckTicket.SolidVolume));
            var solidFixedValidationMessage = $"Solids quantity is outside the range allowed for the selected Service Type {ServiceType.ServiceTypeId}";

            if (ValidateVolumeCutValueRange(TruckTicket.SolidVolume, ServiceType.SolidMinValue, ServiceType.SolidMaxValue))
            {
                VolumeCutValidationErrors.TryAdd(nameof(TruckTicket.SolidVolume), solidFixedValidationMessage);
            }
        }
        else if (ServiceType.IncludesSolids && ServiceType.SolidThresholdType == SubstanceThresholdType.Percentage)
        {
            VolumeCutValidationErrors.Remove(nameof(TruckTicket.SolidVolumePercent));
            var solidPercentageValidationMessage = $"Solids percentage is outside the range allowed for the selected Service Type {ServiceType.ServiceTypeId}";

            if (ValidateVolumeCutValueRange(TruckTicket.SolidVolumePercent, ServiceType.SolidMinValue, ServiceType.SolidMaxValue))
            {
                VolumeCutValidationErrors.TryAdd(nameof(TruckTicket.SolidVolumePercent), solidPercentageValidationMessage);
            }
        }

        TriggerStateChanged();
    }

    private static bool ValidateVolumeCutValueRange(double cutValue, double? cutMinValue, double? cutMaxValue)
    {
        return (cutMinValue.HasValue && cutMaxValue.HasValue && (cutValue < cutMinValue || cutValue > cutMaxValue))
            || (cutMinValue.HasValue && !cutMaxValue.HasValue && cutValue < cutMinValue)
            || (!cutMinValue.HasValue && cutMaxValue.HasValue && cutValue > cutMaxValue);
    }

    public Contracts.Models.Operations.MaterialApproval CreateNewMaterialApproval()
    {
        var materialApproval = new Contracts.Models.Operations.MaterialApproval();
        materialApproval.FacilityId = Facility?.Id ?? TruckTicket.FacilityId;
        materialApproval.Facility = Facility?.Name ?? TruckTicket.FacilityName;
        materialApproval.LegalEntity = TruckTicket.LegalEntity;
        materialApproval.LegalEntityId = TruckTicket.LegalEntityId;
        materialApproval.CountryCode = TruckTicket.CountryCode;
        materialApproval.SourceLocationId = SourceLocation?.Id ?? TruckTicket.SourceLocationId;
        materialApproval.SourceLocation = SourceLocation?.SourceLocationName ?? TruckTicket.SourceLocationName;
        materialApproval.SourceLocationFormattedIdentifier = SourceLocation?.FormattedIdentifier ?? TruckTicket.SourceLocationFormatted;
        materialApproval.SourceLocationUnformattedIdentifier = SourceLocation?.Identifier ?? TruckTicket.SourceLocationUnformatted;
        materialApproval.GeneratorId = SourceLocation.GeneratorId;
        materialApproval.GeneratorName = SourceLocation.GeneratorName;

        return materialApproval;
    }
}
