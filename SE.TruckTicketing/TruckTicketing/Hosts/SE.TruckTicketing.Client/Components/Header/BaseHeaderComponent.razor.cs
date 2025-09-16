using Microsoft.AspNetCore.Components;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Header;

public partial class BaseHeaderComponent : BaseRazorComponent
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public EventCallback Click { get; set; }
}
