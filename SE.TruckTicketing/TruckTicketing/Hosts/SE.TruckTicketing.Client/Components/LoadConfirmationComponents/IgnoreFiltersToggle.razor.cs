using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class IgnoreFiltersToggle : FilterComponent<bool>
{
    private bool _ignoreFilters;

    private string IgnoreFiltersText => "Ignore Status and Frequency filters.";

    [Inject]
    public TooltipService TooltipService { get; set; }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _ignoreFilters = false;
        criteria.Filters["IgnoreFilters"] = false;
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (_ignoreFilters)
        {
            criteria.Filters["IgnoreFilters"] = true;
        }
        else
        {
            criteria.Filters.Remove("IgnoreFilters");
        }
    }

    protected async Task HandleChange()
    {
        await PropagateFilterValueChange(_ignoreFilters);
    }

    private void ShowIgnoreTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, IgnoreFiltersText, options);
    }
}

