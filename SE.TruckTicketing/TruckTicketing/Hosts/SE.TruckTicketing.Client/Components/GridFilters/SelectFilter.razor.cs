using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Trident.Api.Search;
using Trident.Search;
using Trident.UI.Blazor.Components.Forms;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class SelectFilter : FilterComponent<string[]>
{
    private object _value;

    [Parameter]
    public SelectOption[] Data { get; set; }

    [Parameter]
    public CompareOperators CompareOperator { get; set; } = CompareOperators.eq;

    private async Task HandleChange(object args)
    {
        _value = (args as IEnumerable<string>)?.ToArray();
        await PropagateFilterValueChange((args as IEnumerable<string>)?.ToArray());
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _value = default;
        
        criteria?.Filters?.Remove(FilterPath);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        var values = (_value as IEnumerable<string>)?.ToArray() ?? Array.Empty<string>();
        if (!values.Any())
        {
            criteria.Filters.Remove(FilterPath);
            return;
        }

        IJunction query = AxiomFilterBuilder.CreateFilter().StartGroup();

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
}

public static class SelectFilterExtensions
{
    public static SelectOption[] SelectOptions<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, List<string> include = null)
    {
        if (include != null)
        {
            return dictionary.Select(kvp => new SelectOption
            {
                Id = kvp.Key.ToString(),
                Text = kvp.Value.ToString(),
            }).Where(opt => include.Contains(opt.Id)).ToArray();
        }
        
        return dictionary.Select(kvp => new SelectOption
        {
            Id = kvp.Key.ToString(),
            Text = kvp.Value.ToString(),
        }).ToArray();
    }
}
