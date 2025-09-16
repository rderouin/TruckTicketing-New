using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components;

public partial class ToggleFilter : FilterComponent<bool>
{
    private bool _enableFilter;

    [Parameter]
    public string ToolTipText { get; set; }

    [Parameter]
    public string FilterKey { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _enableFilter = false;
        criteria.Filters[FilterKey] = false;
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (_enableFilter)
        {
            criteria.Filters[FilterKey] = true;
        }
        else
        {
            criteria.Filters.Remove(FilterKey);
        }
    }

    protected async Task HandleChange()
    {
        await PropagateFilterValueChange(_enableFilter);
    }

    private void ShowTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, ToolTipText, options);
    }
}
