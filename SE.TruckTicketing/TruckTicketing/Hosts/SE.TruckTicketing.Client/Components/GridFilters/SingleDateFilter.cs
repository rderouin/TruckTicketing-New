using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Trident.Api.Search;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class SingleDateFilter : RangeFilterComponent<DateTime>
{
    protected new bool IsValid = true;

    [Parameter]
    public string Format { get; set; } = "d";

    [Parameter]
    public CompareOperators SelectedOperator { get; set; } = CompareOperators.lte;

    [Parameter]
    public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Unspecified;

    private async Task HandleDateChange(DateTime? _)
    {
        await Propagate();
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        Start = default;
        criteria?.Filters?.Remove(FilterPath);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (Start is null)
        {
            criteria?.Filters?.Remove(FilterPath);
            return;
        }

        IJunction query = AxiomFilterBuilder.CreateFilter().StartGroup();
        var index = 0;

        if (Start is not null)
        {
            if (SelectedOperator == CompareOperators.lt)//an end date
            {
                var lessThanDate = Start.Value.Date.AddDays(1);

                query = AddDateToQuery(lessThanDate, query, index);
            }
            else
            {
                query = AddDateToQuery(Start.Value.Date, query, index);
            }
        }

        var filter = ((AxiomTokenizer)query).EndGroup().Build();

        if (filter != null)
        {
            criteria.Filters ??= new();
            criteria.Filters[FilterPath] = filter;
        }
    }

    private IJunction AddDateToQuery(DateTime selectedDate, IJunction query, int index)
    {
        if (DateTimeKind.Equals(DateTimeKind.Utc))
        {
            selectedDate = new(selectedDate.Year, selectedDate.Month, selectedDate.Day, selectedDate.Hour, selectedDate.Minute, selectedDate.Second, selectedDate.Millisecond, DateTimeKind.Utc);
        }

        query = ((GroupStart)query).AddAxiom(new()
        {
            Field = FilterPath,
            Key = $"{FilterPath}{++index}",
            Operator = SelectedOperator,
            Value = selectedDate,
        });

        return query;
    }
}
