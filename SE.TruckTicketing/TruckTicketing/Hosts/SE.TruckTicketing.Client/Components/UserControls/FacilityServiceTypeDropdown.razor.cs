using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class FacilityServiceTypeDropdown<TValue>
{
    private SearchResultsModel<FacilityService, SearchCriteriaModel> _facilityservices = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<FacilityService>(),
    };

    private SearchResultsModel<ServiceType, SearchCriteriaModel> _serviceTypes = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<ServiceType>(),
    };

    [Inject]
    public IServiceBase<FacilityService, Guid> FacilityServiceService { get; set; }

    [Inject]
    public IServiceBase<ServiceType, Guid> ServiceTypeService { get; set; }

    [Parameter]
    public List<Guid> FacilityIds { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    private async Task LoadData(LoadDataArgs args)
    {
        var criteria = args.ToSearchCriteriaModel();
        await GetData(criteria);
        EnsureBoundValueIsIncludedInData();

        StateHasChanged();
    }

    public override async Task Refresh(object facilityIds)
    {
        var ids = facilityIds as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();
        var pageSize = 10;
        IsLoading = true;
        StateHasChanged();

        if (facilityIds != null)
        {
            FacilityIds = (List<Guid>)ids;
        }

        await LoadData(new() { Top = PageSize == 0 ? 10 : pageSize });

        IsLoading = false;
        StateHasChanged();
    }

    protected override void EnsureBoundValueIsIncludedInData()
    {
        Data = _serviceTypes.Results?.ToList() ?? Data;

        if (TypedSelectedItem is not null && Data.All(item => item.Id != TypedSelectedItem.Id))
        {
            Data.Add(TypedSelectedItem);
        }

        Count = _serviceTypes.Info?.TotalRecords ?? Math.Max(0, Data.Count);
    }

    protected override async Task OnLoadData(LoadDataArgs args)
    {
        await LoadData(args);
    }

    private async Task GetData(SearchCriteriaModel criteria)
    {
        var searchCriteria = new SearchCriteriaModel();
        searchCriteria.PageSize = 100;
        searchCriteria.Filters[nameof(FacilityService.IsActive)] = true;
        if (FacilityIds.Any())
        {
            searchCriteria.Filters[nameof(FacilityService.FacilityId)] =
                FacilityIds.AsInclusionAxiomFilter(nameof(FacilityService.FacilityId), CompareOperators.eq);
        }

        _facilityservices = await FacilityServiceService.Search(searchCriteria);

        var serviceIds = _facilityservices?.Results?.DistinctBy(s => s.ServiceTypeId).Select(st => st.ServiceTypeId).ToList();

        if (serviceIds != null && serviceIds.Any())
        {
            criteria.Filters[nameof(ServiceType.SearchableId)] =
                serviceIds.AsInclusionAxiomFilter(nameof(ServiceType.SearchableId), CompareOperators.eq);
        }

        if (criteria.Keywords.HasText())
        {
            criteria.Filters["SearchByKeyword"] =
                new AxiomModel
                {
                    Value = criteria.Keywords.ToLower(),
                    Field = nameof(ServiceType.Name),
                    Key = $"{nameof(ServiceType.Name)}1",
                    Operator = Trident.Api.Search.CompareOperators.contains,
                };
        }

        _serviceTypes = await ServiceTypeService.Search(criteria);
    }
}
