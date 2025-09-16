using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Search;
using Trident.UI.Blazor.Components.Forms;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class CheckBoxListSelectFilter<TOutputValue> : FilterComponent<IEnumerable<TOutputValue>>
{
    [Parameter]
    public CompareOperators CompareOperator { get; set; } = CompareOperators.eq;

    private IEnumerable<TOutputValue> Value { get; set; }
    [Parameter]
    public List<ListOption<TOutputValue>> Data { get; set; }

    public override void Reset(SearchCriteriaModel criteria)
    {
        Value = default;
        criteria?.Filters?.Remove(FilterPath);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        var values = Value?.ToArray() ?? Array.Empty<TOutputValue>();
        if (!values.Any())
        {
            criteria.Filters.Remove(FilterPath);
            return;
        }

        IJunction query = AxiomFilterBuilder.CreateFilter()
                                            .StartGroup();

        var index = 0;
        foreach (var value in values)
        {
            if (query is GroupStart groupStart)
            {
                query = groupStart.AddAxiom(new()
                {
                    Key = $"{FilterPath}{++index}".Replace(".", string.Empty),
                    Field = FilterPath,
                    Operator = CompareOperator,
                    Value = value,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = $"{FilterPath}{++index}".Replace(".", string.Empty),
                    Field = FilterPath,
                    Operator = CompareOperator,
                    Value = value,
                });
            }

            criteria.Filters[FilterPath] = ((AxiomTokenizer)query).EndGroup().Build();
        }
    }

    private async Task HandleChange()
    {
        await PropagateFilterValueChange(Value.ToArray());
    }

}
