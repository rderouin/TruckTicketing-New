using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.UI.ViewModels;

public class NavigationMenuItem
{
    public Guid? Id { get; set; }

    public string Path { get; set; }

    public string Text { get; set; }

    public string Icon { get; set; }

    public List<NavigationMenuItem> SubMenus { get; set; } = new();

    public string ClaimName { get; set; }

    public string ClaimValue { get; set; }

    public string BlazorPageRoute { get; set; }

    public int Order { get; set; }
}
