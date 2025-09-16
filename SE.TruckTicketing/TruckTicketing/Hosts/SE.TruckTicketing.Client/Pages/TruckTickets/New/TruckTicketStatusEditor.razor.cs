using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class TruckTicketStatusEditor : BaseRazorComponent
{
    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }
    
    private TruckTicketStatus TruckTicketStatus { get; set; }

    public override void Dispose()
    {
        ViewModel.StateChanged -= StateChange;
    }

    protected override void OnInitialized()
    {
        ViewModel.StateChanged += StateChange;
    }
    
    private async Task StateChange()
    {
        await InvokeAsync(StateHasChanged);
    }
    
    private void HandleStatusSelection(TruckTicketStatus status)
    {
        ViewModel.SetTruckTicketStatus(status);
    }
    
    
}
