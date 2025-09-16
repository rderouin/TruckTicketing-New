using System.Collections.Generic;

namespace SE.TruckTicketing.Client.Configuration.Navigation;

public class UserNavigationContext
{
    public UserNavigationContext(List<NavigationItem> navigationItems)
    {
        NavigationItems = navigationItems;
    }

    public List<NavigationItem> NavigationItems { get; set; }
}
