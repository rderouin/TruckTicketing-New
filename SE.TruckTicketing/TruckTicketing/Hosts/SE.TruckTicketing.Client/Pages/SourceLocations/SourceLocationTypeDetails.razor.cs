using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.ViewModels.SourceLocations;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages.SourceLocations;

public partial class SourceLocationTypeDetails : BaseTruckTicketingComponent
{
    protected bool IsBusy;

    private bool DisableSourceLocationCodeMaskSwitch => !ViewModel.SourceLocationType.SourceLocationCodeMask.HasText();

    [Parameter]
    public SourceLocationTypeDetailsViewModel ViewModel { get; set; }

    [Parameter]
    public EventCallback<SourceLocationType> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleSubmit()
    {
        IsBusy = true;
        await OnSubmit.InvokeAsync(ViewModel.SourceLocationType);
        IsBusy = false;
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }
}
