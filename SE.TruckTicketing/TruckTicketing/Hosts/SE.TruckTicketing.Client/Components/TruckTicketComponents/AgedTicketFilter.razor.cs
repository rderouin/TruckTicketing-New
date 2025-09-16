using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components.GridFilters;

using Trident.Api.Search;
using Trident.Contracts.Configuration;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class AgedTicketFilter : FilterComponent<bool>
{
    private int _agedTicketDaysThreshold;

    private bool _showAgedTickets;

    [Inject]
    public IAppSettings AppSettings { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    private string AgedTicketsText => $"Show Void and Invoiced tickets older than {_agedTicketDaysThreshold} days.";

    public override void Reset(SearchCriteriaModel criteria)
    {
        _showAgedTickets = false;
        criteria.Filters["Aged"] = _agedTicketDaysThreshold;
    }

    public override void ApplyFilter(SearchCriteriaModel criteria)
    {
        if (!_showAgedTickets)
        {
            criteria.Filters["Aged"] = _agedTicketDaysThreshold;
        }
        else
        {
            criteria.Filters.Remove("Aged");
        }
    }

    protected async Task HandleChange()
    {
        await PropagateFilterValueChange(_showAgedTickets);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (!int.TryParse(AppSettings["Values:AgedTicketDaysThreshold"], out _agedTicketDaysThreshold))
            {
                _agedTicketDaysThreshold = 120;
            }
        }
    }

    private void ShowAgedTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, AgedTicketsText, options);
    }
}
