using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Models.Substances;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.InvoiceConfigurationComponents;

public partial class InvoiceConfigurationPermutationGrid : BaseTruckTicketingComponent
{
    private List<string> _allFacilities = new();

    private List<string> _allServiceTypes = new();

    private List<string> _allSourceLocations = new();

    private List<string> _allSubstances = new();

    private List<string> _allWellClassifications = new();

    private List<InvoiceConfigurationPermutations> _generatedPermutations = new();

    private PagableGridView<InvoiceConfigurationPermutations> _grid;

    private SearchResultsModel<InvoiceConfigurationPermutations, SearchCriteriaModel> _invoiceConfigurationPermutations = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<InvoiceConfigurationPermutations>(),
    };

    private bool _isLoading;

    private Dictionary<string, List<string>> _previewInvoiceNumbers = new();

    private bool isGlobalView = false;

    private bool _disableValidateSetUp => InvoiceConfig.Id == Guid.Empty || Operation == "clone";

    [Inject]
    private IServiceBase<InvoiceConfigurationPermutationsIndex, Guid> InvoiceConfigurationPermutationIndexService { get; set; }

    [Inject]
    private IInvoiceConfigurationService InvoiceConfigurationService { get; set; }

    [Inject]
    private IServiceBase<SourceLocation, Guid> SourceLocationService { get; set; }

    [Inject]
    private IServiceBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    private IServiceBase<ServiceType, Guid> ServiceTypeService { get; set; }

    [Inject]
    private IServiceBase<Substance, Guid> SubstanceService { get; set; }

    [Parameter]
    public InvoiceConfiguration InvoiceConfig { get; set; }

    [Parameter]
    public bool IsInvoiceInfoSelectionChanged { get; set; }

    [Parameter]
    public bool IsInvoiceSplittingChanged { get; set; }

    [Parameter]
    public string Operation { get; set; }

    [Parameter]
    public EventCallback<List<BillingConfiguration>> InvalidBillingConfiguration { get; set; }

    [Parameter]
    public int PermutationsLimit { get; set; } = 2;

    private bool GlobalViewDisabled => InvoiceConfig.CustomerId == default;

    protected override async Task OnInitializedAsync()
    {
        _allWellClassifications = DataDictionary.For<WellClassifications>().Select(x => x.Value).ToList();
        await LoadSourceLocations();
        await LoadServiceTypes();
        await LoadSubstances();
        await LoadFacilities();
        await base.OnInitializedAsync();
    }

    private void GeneratePreviewInvoiceNumber()
    {
        const int seed = 10000000;
        var lastNumber = 10000000;

        foreach (var facilityCode in _generatedPermutations.GroupBy(x => x.Facility).Select(group => new
                 {
                     FacilityCode = group.Key,
                     Count = group.Count(),
                 }))
        {
            foreach (var permutation in _generatedPermutations.Where(x => x.Facility == facilityCode.FacilityCode))
            {
                lastNumber += 1;
                permutation.Number = $"{facilityCode.FacilityCode}{lastNumber}-IV";
            }

            lastNumber = seed;
        }
    }

    private async Task LoadSourceLocations()
    {
        var sourceLocations = await SourceLocationService.Search(new()
        {
            PageSize = 2,
            OrderBy = nameof(SourceLocation.FormattedIdentifier),
            Filters =
            {
                [nameof(SourceLocation.IsActive)] = true,
            },
        });

        _allSourceLocations = sourceLocations?.Results?.Select(x => x.Display).ToList();
    }

    private async Task LoadServiceTypes()
    {
        var serviceType = await ServiceTypeService.Search(new()
        {
            PageSize = 2,
            OrderBy = nameof(ServiceType.Name),
            Filters =
            {
                [nameof(ServiceType.IsActive)] = true,
            },
        });

        _allServiceTypes = serviceType?.Results?.Select(x => x.Name).ToList();
    }

    private async Task LoadSubstances()
    {
        var substance = await SubstanceService.Search(new()
        {
            PageSize = 2,
            OrderBy = nameof(Substance.SubstanceName),
        });

        _allSubstances = substance?.Results?.Select(x => x.SubstanceName).ToList();
    }

    private async Task LoadFacilities()
    {
        var facilities = await FacilityService.Search(new()
        {
            PageSize = 2,
            OrderBy = nameof(Facility.SiteId),
            Filters =
            {
                [nameof(Facility.LegalEntityId)] = InvoiceConfig.CustomerLegalEntityId,
                [nameof(Facility.IsActive)] = true,
            },
        });

        _allFacilities = facilities?.Results?.Select(x => x.SiteId).ToList();
    }

    public async Task ReloadPermutationsGrid()
    {
        await _grid.ReloadGrid();
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadData(new() { PageSize = 10 });

        await base.OnParametersSetAsync();
    }

    private async Task TriggerViewForCustomer()
    {
        await _grid.ReloadGrid();
    }

    private async Task LoadData(SearchCriteriaModel searchCriteria)
    {
        _isLoading = true;
        if (isGlobalView)
        {
            searchCriteria.AddFilter("DocumentType", $"InvoiceConfiguration|{InvoiceConfig.CustomerId}");
            var results = await InvoiceConfigurationPermutationIndexService.Search(searchCriteria);
            if (results != null && results.Results.Any())
            {
                _invoiceConfigurationPermutations.Results = results.Results.Select(x => new InvoiceConfigurationPermutations
                {
                    Name = x.Name,
                    Number = x.Number,
                    SourceLocation = x.SourceLocation,
                    ServiceType = x.ServiceType,
                    Substance = x.Substance,
                    WellClassification = x.WellClassification,
                    Facility = x.Facility,
                }).ToList();
            }

            _invoiceConfigurationPermutations.Info = new() { TotalRecords = results?.Info?.TotalRecords ?? 0 };
        }
        else
        {
            GetInvoiceConfigurationPermutations();
            GeneratePreviewInvoiceNumber();
            var myList = _generatedPermutations.ToList();
            var morePages = searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault() < myList.Count;
            var results = new SearchResultsModel<InvoiceConfigurationPermutations, SearchCriteriaModel>
            {
                Results = myList
                         .Skip(searchCriteria.PageSize.GetValueOrDefault() * searchCriteria.CurrentPage.GetValueOrDefault())
                         .Take(searchCriteria.PageSize.GetValueOrDefault()),
                Info = new()
                {
                    TotalRecords = myList.Count,
                    NextPageCriteria = morePages ? new SearchCriteriaModel { CurrentPage = searchCriteria.CurrentPage + 1 } : null,
                },
            };

            _invoiceConfigurationPermutations = results;
            InvoiceConfig.Permutations = new();
            InvoiceConfig.Permutations.AddRange(_generatedPermutations);
        }

        _isLoading = false;
    }

    private async Task ValidateBillingConfiguration()
    {
        var results = await InvoiceConfigurationService.GetInvalidBillingConfiguration(InvoiceConfig);
        await InvalidBillingConfiguration.InvokeAsync(results ?? new());
    }

    private void GetInvoiceConfigurationPermutations()
    {
        var sourceLocation = new InputGenerator
        {
            ConfigValues = invoice => new(InvoiceConfig.SourceLocationIdentifier?.Take(PermutationsLimit).ToList() ?? new()),
            DbValues = () => _allSourceLocations ?? new(),
            IsSelectAll = config => InvoiceConfig.AllSourceLocations,
            IsSplitBy = config => InvoiceConfig.IsSplitBySourceLocation,
            SetPermutaionItemValue = (item, value) => item.SourceLocation = value,
        };

        var wellClassification = new InputGenerator
        {
            ConfigValues = invoice => new(InvoiceConfig.WellClassifications?.Take(PermutationsLimit).ToList() ?? new()),
            DbValues = () => _allWellClassifications ?? new(),
            IsSelectAll = config => InvoiceConfig.AllWellClassifications,
            IsSplitBy = config => InvoiceConfig.IsSplitByWellClassification,
            SetPermutaionItemValue = (item, value) => item.WellClassification = value,
        };

        var facility = new InputGenerator
        {
            ConfigValues = invoice => new(InvoiceConfig.FacilityCode?.ToList() ?? new()),
            DbValues = () => _allFacilities != null ? _allFacilities.Count > 2 ? _allFacilities.GetRange(0, 2) : _allFacilities : new(),
            IsSelectAll = config => InvoiceConfig.AllFacilities,
            IsSplitBy = config => InvoiceConfig.IsSplitByFacility,
            SetPermutaionItemValue = (item, value) => item.Facility = value,
        };

        var substance = new InputGenerator
        {
            ConfigValues = invoice => new(InvoiceConfig.SubstancesName?.Take(PermutationsLimit).ToList() ?? new()),
            DbValues = () => _allSubstances ?? new(),
            IsSelectAll = config => InvoiceConfig.AllSubstances,
            IsSplitBy = config => InvoiceConfig.IsSplitBySubstance,
            SetPermutaionItemValue = (item, value) => item.Substance = value,
        };

        var serviceType = new InputGenerator
        {
            ConfigValues = invoice => new(InvoiceConfig.ServiceTypesName?.Take(PermutationsLimit).ToList() ?? new()),
            DbValues = () => _allServiceTypes ?? new(),
            IsSelectAll = config => InvoiceConfig.AllServiceTypes,
            IsSplitBy = config => InvoiceConfig.IsSplitByServiceType,
            SetPermutaionItemValue = (item, value) => item.ServiceType = value,
        };

        var inputGenerators = new List<InputGenerator>
        {
            sourceLocation,
            wellClassification,
            facility,
            substance,
            serviceType,
        };

        var sets = inputGenerators.Select(generator => generator.GetPermutationItems(InvoiceConfig)).ToList();
        _generatedPermutations = sets[0];
        foreach (var set in sets.Skip(1))
        {
            _generatedPermutations = _generatedPermutations.SelectMany(item => item.CrossApply(set)).ToList();
        }
    }

    public class InputGenerator
    {
        public Func<InvoiceConfiguration, List<string>> ConfigValues { get; set; }

        public Func<List<string>> DbValues { get; set; }

        public Func<InvoiceConfiguration, bool> IsSelectAll { get; set; }

        public Func<InvoiceConfiguration, bool> IsSplitBy { get; set; }

        public Action<InvoiceConfigurationPermutations, string> SetPermutaionItemValue { get; set; }

        private List<string> ValueProvider(InvoiceConfiguration config)
        {
            if (IsSelectAll(config))
            {
                return IsSplitBy(config) ? DbValues() : new() { "All" };
            }

            return ConfigValues(config);
        }

        private List<InvoiceConfigurationPermutations> PermutationProvider(InvoiceConfiguration config, List<string> values)
        {
            if (IsSplitBy(config))
            {
                return values.Select(value =>
                                     {
                                         var item = new InvoiceConfigurationPermutations { Name = config.Name };
                                         SetPermutaionItemValue(item, value);
                                         return item;
                                     }).ToList();
            }

            var item = new InvoiceConfigurationPermutations { Name = config.Name };
            SetPermutaionItemValue(item, string.Join(",", values));
            return new() { item };
        }

        public List<InvoiceConfigurationPermutations> GetPermutationItems(InvoiceConfiguration config)
        {
            var values = ValueProvider(config);
            return PermutationProvider(config, values);
        }
    }
}
