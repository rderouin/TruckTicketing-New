using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Models.Substances;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid.Filters;

namespace SE.TruckTicketing.Client.Components.InvoiceConfigurationComponents;

public partial class InvoiceConfigurationInvoiceInfo : BaseTruckTicketingComponent
{
    private IEnumerable<Facility> _allFacilities;

    private EditContext _editContext;

    private IEnumerable<Guid> _facilities;

    private Guid _facilityId = Guid.Empty;

    private List<Facility> _facilityRecords = new();

    private IEnumerable<Guid> _serviceTypes;

    private IEnumerable<Guid> _sourceLocations;

    private IEnumerable<Guid> _substances;

    private RadzenListBox<Guid> FacilityListBox;

    private SearchResultsModel<Facility, SearchCriteriaModel> FacilityResults = new();

    private bool _enableFacilitySelectAll =>
        InvoiceConfiguration.AllSourceLocations && InvoiceConfiguration.AllSubstances && InvoiceConfiguration.AllServiceTypes &&
        InvoiceConfiguration.AllWellClassifications;

    [Inject]
    private IServiceBase<SourceLocation, Guid> SourceLocationService { get; set; }

    [Inject]
    private IServiceBase<Facility, Guid> FacilityService { get; set; }

    [Parameter]
    public EventCallback<bool> OnInvoiceInfoChanged { get; set; }

    [Parameter]
    public EventCallback<SourceLocation> OnSourceLocationAdded { get; set; }

    [Parameter]
    public EventCallback<SourceLocation> OnSourceLocationDeleted { get; set; }

    [Parameter]
    public EventCallback<bool> OnFacilitySelectionChanged { get; set; }

    private List<ListOption<string>> WellClassificationData { get; set; } = new();

    [Parameter]
    public List<SourceLocation> SourceLocationAdded { get; set; } = new();

    private IEnumerable<string> WellClassification
    {
        get => InvoiceConfiguration.WellClassifications?.Select(x => x.GetEnumDescription<WellClassifications>());
        set
        {
            InvoiceConfiguration.AllWellClassifications = value.Count() == WellClassificationData.Count || !value.Any();
            IsCatchAll();
            InvoiceConfiguration.WellClassifications = new(value.Select(x => x.GetEnumValue<WellClassifications>()).ToList());
        }
    }

    private IEnumerable<Guid> Facilities
    {
        get => _facilities ?? InvoiceConfiguration.Facilities;
        set
        {
            InvoiceConfiguration.AllFacilities = !value.Any();
            IsCatchAll();
            InvoiceConfiguration.Facilities = new(value.ToList());
            _facilities = value;
        }
    }

    private IEnumerable<Guid> SourceLocations
    {
        get => _sourceLocations ?? InvoiceConfiguration.SourceLocations;
        set
        {
            InvoiceConfiguration.AllSourceLocations = !value.Any();
            InvoiceConfiguration.SourceLocationIdentifier = new();
            IsCatchAll();
            foreach (var sourceLocation in value)
            {
                InvoiceConfiguration.SourceLocationIdentifier.Add(SourceLocationAdded.First(x => x.Id == sourceLocation).FormattedIdentifier);
            }

            InvoiceConfiguration.SourceLocations = new(value.ToList());
            _sourceLocations = value;
        }
    }

    private IEnumerable<Guid> Substances
    {
        get => _substances ?? InvoiceConfiguration.Substances;
        set
        {
            InvoiceConfiguration.AllSubstances = !value.Any();
            IsCatchAll();
            InvoiceConfiguration.Substances = new(value.ToList());
            _substances = value;
        }
    }

    private IEnumerable<Guid> ServiceTypes
    {
        get => _serviceTypes ?? InvoiceConfiguration.ServiceTypes;
        set
        {
            InvoiceConfiguration.AllServiceTypes = !value.Any();
            IsCatchAll();
            InvoiceConfiguration.ServiceTypes = new(value.ToList());
            _serviceTypes = value;
        }
    }

    [Parameter]
    public InvoiceConfiguration InvoiceConfiguration { get; set; }

    private async Task FacilitySelectAll()
    {
        _facilityId = Guid.Empty;
        InvoiceConfiguration.Facilities = new();
        InvoiceConfiguration.FacilityCode = new();
        IsCatchAll();
        await OnFacilitySelectionChanged.InvokeAsync();
    }

    private async Task OnFacilityChange()
    {
        if (FacilityListBox?.SelectedItem is not Facility selectedFacility)
        {
            return;
        }

        InvoiceConfiguration.AllFacilities = false;
        _facilityId = selectedFacility.Id;
        InvoiceConfiguration.Facilities = new();
        InvoiceConfiguration.FacilityCode = new();
        IsCatchAll();
        InvoiceConfiguration.FacilityCode.Add(selectedFacility.SiteId);
        IEnumerable<Guid> facilitiesList = new List<Guid> { selectedFacility.Id };
        InvoiceConfiguration.Facilities = new(facilitiesList.ToList());
        await OnFacilitySelectionChanged.InvokeAsync();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnInvoiceInfoChanged.InvokeAsync(_editContext.IsModified());
    }

    protected override async Task OnInitializedAsync()
    {
        _editContext = new(InvoiceConfiguration);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;

        WellClassificationData = DataDictionary.For<WellClassifications>().Select(x =>
                                                                                      new ListOption<string>
                                                                                      {
                                                                                          Display = x.Value,
                                                                                          Value = x.Value,
                                                                                      }).ToList();

        _facilityId = InvoiceConfiguration.Facilities != null && InvoiceConfiguration.Facilities.Any() ? InvoiceConfiguration.Facilities.First() : Guid.Empty;
        await LoadFacilityData(new());
        await base.OnInitializedAsync();
    }

    private async Task LoadFacilityData(LoadDataArgs args)
    {
        var searchCriteriaModel = args.ToSearchCriteriaModel();
        BeforeFacilityLoad(searchCriteriaModel);
        FacilityResults = await FacilityService!.Search(searchCriteriaModel)!;
        _facilityRecords = FacilityResults?.Results?.ToList();
        if (_facilityId != Guid.Empty && _facilityRecords != null && _facilityRecords.Any())
        {
            _facilityRecords = _facilityRecords.OrderByDescending(x => x.Id == _facilityId).ToList();
        }
    }

    private void GetAllFacilities(IEnumerable<Facility> allFacilities)
    {
        if (allFacilities == null)
        {
            return;
        }

        var selectedFacilities = allFacilities.ToList();
        _allFacilities = selectedFacilities;
        if (InvoiceConfiguration.Id == default || InvoiceConfiguration.AllFacilities || (InvoiceConfiguration.AllFacilities && InvoiceConfiguration.IsSplitByFacility))
        {
            _facilities = selectedFacilities.ToList().Select(x => x.Id);
            InvoiceConfiguration.FacilityCode = new();
            foreach (var selectedFacility in selectedFacilities)
            {
                InvoiceConfiguration.FacilityCode.Add(selectedFacility.SiteId);
            }
        }
    }

    private void BeforeFacilityLoad(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(Facility.SiteId);
        criteria.Filters[nameof(Facility.LegalEntityId)] = InvoiceConfiguration.CustomerLegalEntityId;
        criteria.Filters[nameof(Facility.IsActive)] = true;
        criteria.PageSize = Int32.MaxValue;
    }

    private void BeforeSubstanceLoad(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(Substance.SubstanceName);
        criteria.PageSize = Int32.MaxValue;
    }

    private void BeforeServiceTypeLoad(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(ServiceType.Name);
        criteria.PageSize = Int32.MaxValue;
        criteria.Filters[nameof(ServiceType.IsActive)] = true;
    }

    private void AllFacilitiesSet(bool isAllSet)
    {
        InvoiceConfiguration.AllFacilities = isAllSet;
        IsCatchAll();
    }

    private void AllSubstancesSet(bool isAllSet)
    {
        InvoiceConfiguration.AllSubstances = isAllSet;
    }

    private void IsFacilityLoaded(bool isLoaded)
    {
        if (isLoaded)
        {
            OnInvoiceInfoChanged.InvokeAsync(true);
        }
    }

    private void AllServiceTypesSet(bool isAllSet)
    {
        InvoiceConfiguration.AllServiceTypes = isAllSet;
    }

    private void IsCatchAll()
    {
        InvoiceConfiguration.CatchAll = InvoiceConfiguration.AllWellClassifications && InvoiceConfiguration.AllFacilities && InvoiceConfiguration.AllSubstances &&
                                        InvoiceConfiguration.AllServiceTypes && InvoiceConfiguration.AllSourceLocations;

        InvoiceConfiguration.AllFacilities = InvoiceConfiguration.CatchAll;
    }

    private async Task AddSourceLocationData(SourceLocation sourceLocation)
    {
        if (InvoiceConfiguration.SourceLocations == null || !InvoiceConfiguration.SourceLocations.Contains(sourceLocation.Id))
        {
            SourceLocationAdded.Add(sourceLocation);
            InvoiceConfiguration.SourceLocationIdentifier ??= new();
            var sourceLocationDisplay = sourceLocation.CountryCode == CountryCode.US ? sourceLocation.SourceLocationName : sourceLocation.FormattedIdentifier;
            InvoiceConfiguration.SourceLocationIdentifier.Add(sourceLocationDisplay);
            InvoiceConfiguration.SourceLocations ??= new();
            InvoiceConfiguration.SourceLocations.Add(sourceLocation.Id);
            _sourceLocations = InvoiceConfiguration.SourceLocations;
            InvoiceConfiguration.AllSourceLocations = false;
            IsCatchAll();
            await OnSourceLocationAdded.InvokeAsync(sourceLocation);
            await OnInvoiceInfoChanged.InvokeAsync(true);
        }

        DialogService.Close();
    }

    private async Task SelectSourceLocation()
    {
        await DialogService.OpenAsync<InvoiceInfoSelectSourceLocation>("Select Source Location(s)",
                                                                       new()
                                                                       {
                                                                           { nameof(InvoiceInfoSelectSourceLocation.CustomerLegalEntityId), InvoiceConfiguration.CustomerLegalEntityId },
                                                                           { nameof(InvoiceInfoSelectSourceLocation.OnSelection), new EventCallback<SourceLocation>(this, AddSourceLocationData) },
                                                                       },
                                                                       new()
                                                                       {
                                                                           Width = "60%",
                                                                       });
    }

    private void FetchSelectedFacilitiesItemModel(Dictionary<Guid, Facility> selectedFacilities)
    {
        if (!selectedFacilities.Any())
        {
            InvoiceConfiguration.FacilityCode = new();
            return;
        }

        InvoiceConfiguration.FacilityCode = new();
        foreach (var selectedFacility in selectedFacilities)
        {
            InvoiceConfiguration.FacilityCode.Add(selectedFacility.Value.SiteId);
        }
    }

    private void FetchSelectedSubstancesItemModel(Dictionary<Guid, Substance> selectedSubstances)
    {
        if (!selectedSubstances.Any())
        {
            InvoiceConfiguration.SubstancesName = new();
            return;
        }

        InvoiceConfiguration.SubstancesName = new();
        foreach (var selectedSubstance in selectedSubstances)
        {
            InvoiceConfiguration.SubstancesName.Add(selectedSubstance.Value.SubstanceName);
        }
    }

    private void FetchSelectedServiceTypeItemModel(Dictionary<Guid, ServiceType> selectedServiceTypes)
    {
        if (!selectedServiceTypes.Any())
        {
            InvoiceConfiguration.ServiceTypesName = new();
            return;
        }

        InvoiceConfiguration.ServiceTypesName = new();
        foreach (var selectedServiceType in selectedServiceTypes)
        {
            InvoiceConfiguration.ServiceTypesName.Add(selectedServiceType.Value.Name);
        }
    }

    private async Task DeleteButton_Click(SourceLocation sourceLocation)
    {
        InvoiceConfiguration.SourceLocationIdentifier.Remove(SourceLocationAdded.First(x => x.Id == sourceLocation.Id).FormattedIdentifier);
        SourceLocationAdded.Remove(SourceLocationAdded.First(x => x.Id == sourceLocation.Id));
        InvoiceConfiguration.SourceLocations.Remove(sourceLocation.Id);

        if (_sourceLocations != null && _sourceLocations.Any())
        {
            _sourceLocations.ToList().Remove(sourceLocation.Id);
        }

        if (SourceLocationAdded.Count == 0)
        {
            InvoiceConfiguration.AllSourceLocations = true;
            IsCatchAll();
        }

        await OnSourceLocationDeleted.InvokeAsync(sourceLocation);
        await OnInvoiceInfoChanged.InvokeAsync(true);
    }
}
