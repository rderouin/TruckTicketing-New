using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.BillingConfigurationComponents;

public partial class ActiveBillingConfigurationFilter : FilterComponent<IEnumerable<int>>
{
    private const string ActiveBillingConfigurations = nameof(ActiveBillingConfigurations);

    [Parameter]
    public CompareOperators CompareOperator { get; set; } = CompareOperators.eq;

    private IEnumerable<int> Value { get; set; }

    [Parameter]
    public List<ListOption<int>> Data { get; set; }

    public override void Reset(SearchCriteriaModel criteria)
    {
        Value = default;
        criteria?.Filters?.Remove(ActiveBillingConfigurations);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        var values = Value?.ToArray() ?? Array.Empty<int>();
        if (!values.Any() || values.Length > 1)
        {
            criteria?.Filters?.Remove(ActiveBillingConfigurations);
        }
        else
        {
            criteria?.Filters?.TryAdd(ActiveBillingConfigurations, values);
        }
    }

    private async Task HandleChange()
    {
        await PropagateFilterValueChange(Value.ToArray());
    }
}
