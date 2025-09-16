using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class ReadyToSendFilter : FilterComponent<bool>
{
    private bool _showOnDemandLoadConfirmations;

    private string OnDemandLoadConfirmationsText => "Show On Demand load confirmations.";

    [Inject]
    public TooltipService TooltipService { get; set; }

    public override void Reset(SearchCriteriaModel criteria)
    {
        _showOnDemandLoadConfirmations = false;
        criteria.Filters["OnDemand"] = false;
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (_showOnDemandLoadConfirmations)
        {
            criteria.Filters["OnDemand"] = true;
        }
        else
        {
            criteria.Filters.Remove("OnDemand");
        }
    }

    protected async Task HandleChange()
    {
        await PropagateFilterValueChange(_showOnDemandLoadConfirmations);
    }

    private void ShowAgedTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, OnDemandLoadConfirmationsText, options);
    }
}
