using System;
using System.Threading.Tasks;

using Trident.Api.Search;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public abstract class RangeFilterComponent<TValue> : FilterComponent<(TValue? start, TValue? end)>
    where TValue : struct, IComparable
{
    protected TValue? Start { get; set; }

    protected TValue? End { get; set; }

    protected bool IsValid => !Start.HasValue || !End.HasValue || Start.Value.CompareTo(End.Value) <= 0;

    protected async Task Propagate()
    {
        if (IsValid)
        {
            await PropagateFilterValueChange((Start, End));
        }
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        Start = default;
        End = default;

        criteria?.Filters?.Remove(FilterPath);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (Start is null && End is null)
        {
            criteria?.Filters?.Remove(FilterPath);
            return;
        }

        IJunction query = AxiomFilterBuilder.CreateFilter().StartGroup();
        var index = 0;

        if (Start is not null)
        {
            query = ((GroupStart)query).AddAxiom(new()
            {
                Field = FilterPath,
                Key = $"{FilterPath}{++index}",
                Operator = CompareOperators.gte,
                Value = Start,
            });
        }

        if (Start is not null && End is not null)
        {
            query = ((AxiomTokenizer)query).And();
        }

        if (End is not null)
        {
            if (query is ILogicalOperator and)
            {
                query = and.AddAxiom(new()
                {
                    Field = FilterPath,
                    Key = $"{FilterPath}{++index}",
                    Operator = CompareOperators.lte,
                    Value = (End is DateTime dt) ? (End as DateTime?)!.Value.AddDays(1).AddMilliseconds(-1) : End,
                });
            }
            else
            {
                query = ((GroupStart)query).AddAxiom(new()
                {
                    Field = FilterPath,
                    Key = $"{FilterPath}{++index}",
                    Operator = CompareOperators.lte,
                    Value = End,
                });
            }
        }

        var filter = ((AxiomTokenizer)query)
                    .EndGroup()
                    .Build();

        if (filter != null)
        {
            criteria.Filters ??= new();
            criteria.Filters[FilterPath] = filter;
        }
    }
}
