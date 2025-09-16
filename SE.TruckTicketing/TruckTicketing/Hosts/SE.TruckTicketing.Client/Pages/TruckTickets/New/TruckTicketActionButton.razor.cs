using System;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class TruckTicketActionButton : BaseTruckTicketingComponent
{
    private dynamic _dialogResult;

    [Parameter]
    public TruckTicket TruckTicket { get; set; }

    [Parameter]
    public TruckTicketStatus? CurrentStatus { get; set; }

    [Parameter]
    public bool Disabled { get; set; }
    
    
    [Parameter]
    public int SalesLineCount { get; set; }

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public EventCallback<TruckTicketStatus> OnStatusChange { get; set; }
    [Inject]
    private NotificationService NotificationService { get; set; }
    private TruckTicketStatus? PrimaryTransitionableStatus => TransitionableStatuses.FirstOrDefault();

    private TruckTicketStatus[] SecondaryTransitionableStatuses => TransitionableStatuses.Skip(1).ToArray();

    private TruckTicketStatus[] TransitionableStatuses =>
        CurrentStatus switch
        {
            TruckTicketStatus.Stub => new[] { TruckTicketStatus.Open, TruckTicketStatus.Void },
            TruckTicketStatus.New => new[] { TruckTicketStatus.Open, TruckTicketStatus.Void },
            TruckTicketStatus.Open => SalesLineCount > 0 ? new[] { TruckTicketStatus.Approved, TruckTicketStatus.Hold, TruckTicketStatus.Void } :  new[] { TruckTicketStatus.Hold, TruckTicketStatus.Void } ,
            TruckTicketStatus.Hold =>  SalesLineCount > 0 ? new[] { TruckTicketStatus.Open, TruckTicketStatus.Approved, TruckTicketStatus.Void } : new[] { TruckTicketStatus.Open, TruckTicketStatus.Void },
            TruckTicketStatus.Approved => new[] { TruckTicketStatus.Void },
            _ => Array.Empty<TruckTicketStatus>(),
        };

    private string GetActionText(TruckTicketStatus? status)
    {
        var action = status switch
                     {
                         TruckTicketStatus.Open => "Open",
                         TruckTicketStatus.Hold => "Hold",
                         TruckTicketStatus.Approved => "Approve",
                         TruckTicketStatus.Void => "Void",
                         _ => "",
                     };

        return action;
    }

    private async Task HandleTransition(RadzenSplitButtonItem item)
    {
        if (item != null)
        {
            await HandleTransition(item.Value);
        }
        else
        {
            await HandleTransition(PrimaryTransitionableStatus.Value.ToString());
        }
    }

    private async Task HandleTransition(string newStatus)
    {
        var status = Enum.Parse<TruckTicketStatus>(newStatus);
        if (status is TruckTicketStatus.Hold or TruckTicketStatus.Void)
        {
            if (SalesLineCount > 0 && CurrentStatus == TruckTicketStatus.Approved)
            {
                NotificationService.Notify(NotificationSeverity.Error, detail: $"Please make sure to first (R)remove any sales lines from the load confirmation before ${status.Humanize()}ing this Approved ticket ticket.");
                return;
            }

            _dialogResult = await DialogService.OpenAsync<TruckTicketStatusReason>($"Specify {status} Reason", new()
            {
                [nameof(TruckTicketStatusReason.TruckTicket)] = TruckTicket,
                [nameof(TruckTicketStatusReason.NewStatus)] = status,
                [nameof(TruckTicketStatusReason.OnCancel)] = new EventCallback(this, () => DialogService.Close(_dialogResult)),
                [nameof(TruckTicketStatusReason.OnConfirm)] = new EventCallback(this, async () =>
                                                                                      {
                                                                                          DialogService.Close(_dialogResult);
                                                                                          await OnStatusChange.InvokeAsync(status);
                                                                                      }),
            });
        }
        else if (status is TruckTicketStatus.Open or TruckTicketStatus.Approved)
        {
            TruckTicket.HoldReason = default;
            TruckTicket.VoidReason = default;
            TruckTicket.OtherReason = default;
            await OnStatusChange.InvokeAsync(status);
        }
        else
        {
            await OnStatusChange.InvokeAsync(status);
        }
    }
}
