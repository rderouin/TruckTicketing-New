using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class SalesLineIdsFilter : FilterComponent<bool>
{
    private bool _salesLineIds;

    [Inject]
    public TooltipService TooltipService { get; set; }

    private async Task HandleChange()
    {
        await PropagateFilterValueChange(_salesLineIds);
    }

    private void ShowTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, "Truck Ticket has sales lines.", options);
    }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _salesLineIds = false;

        criteria?.Filters?.Remove("SalesLineIds");
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (_salesLineIds)
        {
            criteria.Filters["SalesLineIds"] = true;
        }
        else
        {
            criteria.Filters.Remove("SalesLineIds");
        }
    }
}
