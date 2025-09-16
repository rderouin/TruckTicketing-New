using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class SelectedYearFilter : FilterComponent<int>
{
    private const string Key = "SelectedYear";

    private static int? _defaultYear = null;

    private int? SelectedYear { get; set; } = _defaultYear;

    [Parameter]
    public IEnumerable<int?> Data { get; set; }

    public override void Reset(SearchCriteriaModel criteria)
    {
        ResetToDefault(criteria);
    }

    protected override void OnInitialized()
    {
        var criteriaModel = base.FilterContext.SearchCriteriaModel;
        ApplyFilter(criteriaModel);
        base.OnInitialized();
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (SelectedYear != null)
        {
            criteria.Filters[Key] = SelectedYear;
        }
        else
        {
            ResetToDefault(criteria);
        }
    }

    protected async Task HandleChange()
    {
        await PropagateFilterValueChange(SelectedYear.GetValueOrDefault());
    }

    private void ResetToDefault(SearchCriteriaModel criteria)
    {
        SelectedYear = _defaultYear;
        criteria.Filters[Key] = SelectedYear;
    }


}
