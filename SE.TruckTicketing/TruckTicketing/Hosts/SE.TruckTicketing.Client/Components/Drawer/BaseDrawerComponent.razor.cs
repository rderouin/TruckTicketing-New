using Microsoft.AspNetCore.Components;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Drawer;

public class BaseDrawerComponent : BaseRazorComponent
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public double OpenSize { get; set; } = 12.5;
}
