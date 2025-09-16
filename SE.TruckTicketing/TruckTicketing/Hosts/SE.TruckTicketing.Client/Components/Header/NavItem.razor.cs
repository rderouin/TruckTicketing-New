using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.Header;

public partial class NavItem
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public bool IsActive { get; set; } = false;

    [Parameter]
    public string Href { get; set; } = "/";
}
