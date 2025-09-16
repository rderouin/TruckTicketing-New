using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Navigation;

public class NavigationModel : GuidApiModelBase
{
    public string ProfileName { get; set; }

    public List<NavigationItemModel> NavigationItems { get; set; }
}

public class NavigationItemModel : GuidApiModelBase
{
    public string RelativeUrl { get; set; }

    public string ClaimType { get; set; }

    public string ClaimValue { get; set; }

    public string Icon { get; set; }

    public string Text { get; set; }

    public int Order { get; set; }

    public List<NavigationItemModel> NavigationItems { get; set; }
}
