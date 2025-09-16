using System;
using System.Collections.Generic;

using Trident.Api.Search;
using Trident.Contracts.Enums;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public class FilterPanelContext
{
    private readonly List<FilterComponent> _filterComponents = new();

    public int PageSize { get; set; }

    public string OrderBy { get; set; }

    public SortOrder SortOrder { get; set; }

    public Action<SearchCriteriaModel> OnFilterChange { get; set; }

    public SearchCriteriaModel SearchCriteriaModel { get; } = new();

    public bool IsExpanded { get; set; }

    public void RegisterFilterComponent(FilterComponent filterComponent)
    {
        _filterComponents.Add(filterComponent);
    }

    public void RaiseChangeEvent()
    {
        SearchCriteriaModel.CurrentPage = 0;
        SearchCriteriaModel.PageSize = PageSize;
        SearchCriteriaModel.OrderBy = OrderBy;
        SearchCriteriaModel.SortOrder = SortOrder;
        OnFilterChange(SearchCriteriaModel);
    }

    public void ResetFilters()
    {
        _filterComponents.ForEach(component => component.Reset(SearchCriteriaModel));
        RaiseChangeEvent();
    }

    public void Reload()
    {
        _filterComponents.ForEach(component => component.ApplyFilter(SearchCriteriaModel));
        RaiseChangeEvent();
    }
}
