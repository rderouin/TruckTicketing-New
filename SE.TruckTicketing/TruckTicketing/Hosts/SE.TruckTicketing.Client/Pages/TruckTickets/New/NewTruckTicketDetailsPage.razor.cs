using System;
using System.Threading.Tasks;

using Humanizer;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketDetailsPage : BaseTruckTicketingComponent
{
    private int _selectedTabIndex;

    private bool IsRefreshing;

    private ElementReference truckTicketPageLoadingContainer;

    private string _backgroundColor => ViewModel.TruckTicket.ServiceTypeClass == Class.Class1 && ViewModel.TruckTicket.TruckTicketType == TruckTicketType.LF ? "rz-badge-danger" : "rz-badge-warning";

    private string _textColor => ViewModel.TruckTicket.ServiceTypeClass == Class.Class1 && ViewModel.TruckTicket.TruckTicketType == TruckTicketType.LF ? "color:white!important" : "";

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Parameter]
    public EventCallback OnRemoveSalesLines { get; set; }

    private TruckTicket TruckTicket => ViewModel.TruckTicket;

    private string Title => ViewModel?.TruckTicket?.Id == Guid.Empty ? $"Create {TruckTicket?.TruckTicketType.Humanize()} Ticket" : "Edit Ticket";

    private TruckTicketStatus? TicketStatus => ViewModel.TruckTicketBackup?.Status ?? ViewModel.TruckTicket?.Status;

    private string Reason =>
        TicketStatus switch
        {
            TruckTicketStatus.Hold => ": " + ViewModel.TruckTicket.HoldReason,
            TruckTicketStatus.Void => ": " + ViewModel.TruckTicket.VoidReason,
            _ => string.Empty,
        };

    private BadgeStyle ValidationStatusBadgeStyle =>
        ViewModel.TruckTicket?.ValidationStatus switch
        {
            TruckTicketValidationStatus.Valid => BadgeStyle.Success,
            TruckTicketValidationStatus.Error => BadgeStyle.Warning,
            _ => BadgeStyle.Light,
        };

    public override void Dispose()
    {
        ViewModel.TruckTicketContainerRefresh -= RefreshTruckTicketContainer;
        ViewModel.Initialized -= ViewModelOnInitialized;
        ViewModel.StateChanged -= StateChange;
    }

    protected override void OnInitialized()
    {
        ViewModel.TruckTicketContainerRefresh += RefreshTruckTicketContainer;
        ViewModel.Initialized += ViewModelOnInitialized;
        ViewModel.StateChanged += StateChange;
    }

    private async Task HandleOnRemoveSalesLines()
    {
        await OnRemoveSalesLines.InvokeAsync();
    }

    private async Task ViewModelOnInitialized()
    {
        _selectedTabIndex = 0;
        await StateChange();
    }

    private async Task RefreshTruckTicketContainer()
    {
        await JSRuntime.InvokeVoidAsync("scrollToTop", truckTicketPageLoadingContainer);
    }

    private async Task StateChange()
    {
        await InvokeAsync(StateHasChanged);
    }

    private void ShowTooltip(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, "Refresh Truck Ticket.", options);
    }

    private async Task RefreshTruckTicketDetails()
    {
        IsRefreshing = true;
        ViewModel.IsRefresh = true;
        if (ViewModel.ShowMaterialApproval)
        {
            ViewModel.IsRefreshMaterialApproval = true;
        }

        var truckTicket = await TruckTicketService.GetById(ViewModel.TruckTicket.Id);
        await ViewModel.Initialize(truckTicket);
        IsRefreshing = false;
    }
}
