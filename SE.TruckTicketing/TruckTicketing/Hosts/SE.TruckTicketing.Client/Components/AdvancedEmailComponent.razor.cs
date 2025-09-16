using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.UI.ViewModels;

namespace SE.TruckTicketing.Client.Components;

public partial class AdvancedEmailComponent
{
    [Parameter]
    public AdvancedEmailViewModel Model { get; set; }

    [Parameter]
    public EventCallback<string> OnParameterChange { get; set; }

    [Parameter]
    public EventCallback<AdvancedEmailViewModel> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Model.To = string.Join(";", Model.Contacts.Where(a => a.IsDefault).Select(a => a.Email));
        await base.OnInitializedAsync();
    }

    private async Task Submit()
    {
        if (Model.To.HasText())
        {
            Model.IsOkToProceed = true;
            await OnSubmit.InvokeAsync(Model);
        }
    }

    private async Task Cancel()
    {
        Model.IsOkToProceed = false;
        await OnCancel.InvokeAsync();
    }
}
