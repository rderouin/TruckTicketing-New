using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Lookups;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketVolumeChangesDialog : BaseRazorComponent
{
    private const string FormId = nameof(TruckTicketVolumeChangesDialog);

    private bool CommentsRequired => Model.VolumeChangeReason == VolumeChangeReason.Other;

    private string RequiredClass => CommentsRequired ? "required" : string.Empty;

    [Parameter]
    public Contracts.Models.Operations.TruckTicket Model { get; set; }

    [Parameter]
    public EventCallback<Contracts.Models.Operations.TruckTicket> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnDiscardVolumeChanges { get; set; }

    private async Task HandleSubmit(Contracts.Models.Operations.TruckTicket model)
    {
        model.ResetVolumeFields = false;
        await OnSubmit.InvokeAsync(model);
    }

    private async Task HandleDiscardChanges()
    {
        Model.ResetVolumeFields = true;
        await OnDiscardVolumeChanges.InvokeAsync();
    }
}
