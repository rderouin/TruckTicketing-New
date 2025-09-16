using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;
using Trident.Search;
using Trident.UI.Blazor.Components.Forms;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class SelectedMonthsFilter : FilterComponent<string[]>
{
    private object _value;
    private const string Key = "SelectedMonths";

    private readonly SelectOption[] _data = {
        new(){Id = "1", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(1) },
        new(){Id = "2", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(2) },
        new(){Id = "3", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(3) },
        new(){Id = "4", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(4) },
        new(){Id = "5", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(5) },
        new(){Id = "6", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(6) },
        new(){Id = "7", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(7) },
        new(){Id = "8", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(8) },
        new(){Id = "9", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(9) },
        new(){Id = "10", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(10) },
        new(){Id = "11", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(11) },
        new(){Id = "12", Text = DateTimeFormatInfo.CurrentInfo.GetMonthName(12) },
    };
    
    private async Task HandleChange(object args)
    {
        _value = (args as IEnumerable<string>)?.ToArray();
        await PropagateFilterValueChange((args as IEnumerable<string>)?.ToArray());
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _value = default;

        criteria?.Filters?.Remove(Key);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        var values = (_value as IEnumerable<string>)?.ToArray() ?? Array.Empty<string>();
        if (!values.Any())
        {
            criteria.Filters.Remove(Key);
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
                    Field = Key,
                    Operator = CompareOperators.eq,
                    Value = value,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = $"{FilterPath}{++index}".Replace(".", string.Empty),
                    Field = Key,
                    Operator = CompareOperators.eq,
                    Value = value,
                });
            }

            criteria.Filters[Key] = ((AxiomTokenizer)query).EndGroup().Build();
        }
    }
}
