using System;

using Microsoft.AspNetCore.Components;

using Trident.Api.Search;
using Trident.Contracts.Enums;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class GridFiltersContainer : ComponentBase
{
    private FilterPanelContext _filterContext = new();

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public bool Expandable { get; set; }

    [Parameter]
    public bool Expanded { get; set; }

    [Parameter]
    public Action<SearchCriteriaModel> OnFilterChange { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 10;

    [Parameter]
    public string OrderBy { get; set; }

    [Parameter]
    public SortOrder SortOrder { get; set; }

    private void ToggleExpanded()
    {
        Expanded = !Expanded;
        _filterContext.IsExpanded = !_filterContext.IsExpanded;
    }

    private void ResetFilters()
    {
        _filterContext.ResetFilters();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _filterContext = new()
        {
            OnFilterChange = OnFilterChange,
            PageSize = PageSize,
            OrderBy = OrderBy,
            SortOrder = SortOrder,
        };
    }

    public void Reload()
    {
        _filterContext.Reload();
    }
}
