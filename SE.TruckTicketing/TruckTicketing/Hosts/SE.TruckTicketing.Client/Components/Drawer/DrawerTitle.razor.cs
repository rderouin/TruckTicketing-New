using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.Drawer;

public partial class DrawerTitle
{
    [Parameter]
    public string Text { get; set; } = "Menu";

    [Parameter]
    public string Icon { get; set; } = "menu";

    [Parameter]
    public EventCallback Click { get; set; }
}
