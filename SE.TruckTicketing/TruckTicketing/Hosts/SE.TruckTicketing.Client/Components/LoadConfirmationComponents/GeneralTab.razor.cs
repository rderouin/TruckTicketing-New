using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class GeneralTab : BaseTruckTicketingComponent
{
    [CascadingParameter]
    public LoadConfirmation Model { get; set; }

    [Parameter]
    public bool ShowDetailInTitle { get; set; }

    [Parameter]
    public bool HideSalesLines { get; set; }

    private void BeforeLoadingSalesLines(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(SalesLine.LoadConfirmationId), Model?.Id ?? default);
    }
}
