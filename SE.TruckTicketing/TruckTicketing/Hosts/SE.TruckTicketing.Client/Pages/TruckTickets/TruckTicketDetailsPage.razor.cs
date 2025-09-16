using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.TruckTicketComponents;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketDetailsPage : BaseTruckTicketingComponent
{
    [Parameter]
    public Contracts.Models.Operations.TruckTicket Model { get; set; }

    [Parameter]
    public SearchResultsModel<SalesLine, SearchCriteriaModel> SalesLineResults { get; set; }

    [Parameter]
    public SearchResultsModel<SalesLine, SearchCriteriaModel> SalesLines { get; set; } = new() { Results = new List<SalesLine>() };

    [Parameter]
    public EventCallback OnRefresh { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback LoadSalesLines { get; set; }

    [Parameter]
    public EventCallback LoadPreviewSalesLines { get; set; }

    [Parameter]
    public EventCallback<Contracts.Models.Operations.TruckTicket> CloneTicket { get; set; }

    private string Title => string.IsNullOrEmpty(Model?.TicketNumber) ? "Add Ticket" : "Edit Ticket";

    private bool ShowCloneTicket { get { return Model.TruckTicketType == TruckTicketType.WT && Model.Status == TruckTicketStatus.Approved; } }

    private void OnVoidReasonChange(TruckTicketVoidReason arg)
    {
        StateHasChanged();
    }

    private void OnHoldReasonChange(TruckTicketHoldReason arg)
    {
        StateHasChanged();
    }

    private async Task CloneTruckTicket()
    {
        await DialogService.OpenAsync<TruckTicketCloneComponent>("Truck Ticket Clone",
                                                                 new()
                                                                 {
                                                                     { "Model", Model },
                                                                     {
                                                                         nameof(TruckTicketCloneComponent.CloneTruckTicket), new EventCallback<Contracts.Models.Operations.TruckTicket>(this,
                                                                             new Func<Contracts.Models.Operations.TruckTicket, Task>(async model =>
                                                                                                                                     {
                                                                                                                                         DialogService.Close();
                                                                                                                                         await CloneTicket.InvokeAsync(model);
                                                                                                                                     }))
                                                                     },
                                                                     { nameof(TruckTicketCloneComponent.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                 });
    }

    private void OnStatusChange(object arg)
    {
        if (Enum.Parse<TruckTicketStatus>(arg.ToString()) != TruckTicketStatus.Hold)
        {
            Model.HoldReason = string.Empty;
        }
        else if (Enum.Parse<TruckTicketStatus>(arg.ToString()) != TruckTicketStatus.Void)
        {
            Model.VoidReason = string.Empty;
        }

        StateHasChanged();
    }
}
