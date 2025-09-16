using System.Collections.Generic;

namespace SE.TruckTicketing.Client.Configuration.Navigation;

public class NavigationItem
{
    public string RelativeUrl { get; set; }

    public string Text { get; set; }

    public string Icon { get; set; }

    public string RequiredResource { get; set; }

    public string RequiredOperation { get; set; }

    public bool IsAuthorized { get; set; }

    public bool IsActive { get; set; }

    public NavigationItem Parent { get; set; }

    public List<NavigationItem> Children { get; set; } = new();
}
