using Microsoft.AspNetCore.Components;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components;

public partial class Status : BaseRazorComponent
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string StatusClass { get; set; }
}
