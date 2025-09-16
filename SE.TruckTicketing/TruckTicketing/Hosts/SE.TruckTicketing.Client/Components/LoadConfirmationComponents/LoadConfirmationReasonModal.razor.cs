using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.UI.ViewModels;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class LoadConfirmationReasonModal
{
    [Parameter]
    public LoadConfirmationReasonViewModel Model { get; set; }

    [Parameter]
    public EventCallback OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task Submit()
    {
        if (!Model.CanProceed)
        {
            return;
        }

        Model.IsOkToProceed = true;
        await OnSubmit.InvokeAsync();
    }

    private async Task Cancel()
    {
        Model.IsOkToProceed = false;
        await OnCancel.InvokeAsync();
    }

    private void ReasonChanged()
    {
        StateHasChanged();
    }
}
