using System;
using System.Linq;
using System.Threading.Tasks;

using Trident.Api.Search;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace Trident.UI.Blazor.Components.Grid.Filters;

public abstract class DoubleFilter : FilterComponentBase<DoubleFilterOptions, FilterEventArgs<DoubleFilter>>
{
}

public partial class DoubleFilter<TOutputValue> : DoubleFilter
{
    public DoubleFilter()
    {
        if (!SupportedTypeParameters.Any(x => x == typeof(TOutputValue)))
            throw new ArgumentOutOfRangeException(
                                                  $"Only the following types are supported as the {string.Join(", ", SupportedTypeParameters.Select(x => x.FullName))}");
    }

    public double? LowerBoundValue { get; set; }

    public double? UpperBoundValue { get; set; }

    private static Type[] SupportedTypeParameters { get; } =
    {
        typeof(long), typeof(double),
    };

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
    }

    private async Task ValueChanged(Bounds bound, double? newValue)
    {
        double? origValue;
        if (bound == Bounds.Upper)
        {
            origValue = UpperBoundValue;
            UpperBoundValue = newValue;
        }
        else
        {
            LowerBoundValue = newValue;
            origValue = LowerBoundValue;
        }

        await RaiseOnChangeEvent(origValue, newValue, bound);
    }

    private async Task RaiseOnChangeEvent(object origValue, object newValue, Bounds bound)
    {
        if (OnChange.HasDelegate && await IsValid())
        {
            await OnChange.InvokeAsync(new DoubleFilterArgs(this)
            {
                OriginalValue = origValue,
                NewValue = newValue,
                Bound = bound,
            });

            ;
        }
    }

    public override Task<bool> IsValid()
    {
        var isValid = !(LowerBoundValue > UpperBoundValue);
        return Task.FromResult(isValid);
    }

    protected override async Task ResetToDefault()
    {
        var origValueLower = LowerBoundValue;
        var origValueUpper = UpperBoundValue;

        LowerBoundValue = FilterOptions.LowerBoundDefaultValue.HasValue
                              ? FilterOptions.LowerBoundDefaultValue.GetValueOrDefault()
                              : null;

        await RaiseOnChangeEvent(origValueLower, LowerBoundValue, Bounds.Lower);

        UpperBoundValue = FilterOptions.UpperBoundDefaultValue.HasValue
                              ? FilterOptions.UpperBoundDefaultValue.GetValueOrDefault()
                              : null;

        await RaiseOnChangeEvent(origValueUpper, UpperBoundValue, Bounds.Upper);
    }

    public override void ApplyFilter(SearchCriteriaModel searchCriteria)
    {
        var hasLowerBound = LowerBoundValue.HasValue;
        var hasUpperBound = UpperBoundValue.HasValue;
        var keyIdx = 1;
        var keyBase = FilterOptions.FilterPath;

        if (!hasLowerBound && !hasUpperBound)
        {
            if (searchCriteria.Filters.ContainsKey(FilterOptions.FilterPath))
                searchCriteria.Filters.Remove(FilterOptions.FilterPath);

            return;
        }

        IJunction query = AxiomFilterBuilder
                         .CreateFilter()
                         .StartGroup();

        // lower only 
        if (hasLowerBound)
            query = ((GroupStart)query).AddAxiom(new()
            {
                Field = FilterOptions.FilterPath,
                Key = $"{keyBase}{keyIdx++}",
                Operator = CompareOperators.gte,
                Value = LowerBoundValue.Value,
            });

        if (hasLowerBound && hasUpperBound) query = ((AxiomTokenizer)query).And();

        if (hasUpperBound)
        {
            if (query is ILogicalOperator and)
                query = and.AddAxiom(new()
                {
                    Field = FilterOptions.FilterPath,
                    Key = $"{keyBase}{keyIdx++}",
                    Operator = CompareOperators.lt,
                    Value = UpperBoundValue.Value,
                });
            else
                query = ((GroupStart)query).AddAxiom(new()
                {
                    Field = FilterOptions.FilterPath,
                    Key = $"{keyBase}{keyIdx++}",
                    Operator = CompareOperators.gte,
                    Value = UpperBoundValue.Value,
                });
        }

        var filter = ((AxiomTokenizer)query)
                    .EndGroup()
                    .Build();

        if (filter != null)
        {
            searchCriteria.Filters ??= new();
            searchCriteria.Filters[FilterOptions.FilterPath] = filter;
        }
    }
}