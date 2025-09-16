using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.GridFilters;

public partial class TextFilter : FilterComponent<string>
{
    private string _text;

    [Parameter]
    public string FilterName { get; set; }

    private Task HandleChange()
    {
        return PropagateFilterValueChange(_text);
    }

    private Task Clear()
    {
        _text = default;
        return HandleChange();
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _text = default;
        criteria.Filters?.Remove(FilterName);
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (_text.HasText())
        {
            criteria.Filters ??= new();
            criteria.Filters[FilterName] = _text;
        }
        else
        {
            Reset(criteria);
        }
    }
}
