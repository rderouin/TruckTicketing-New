using Microsoft.AspNetCore.Components;

using WatchListStatusEnum = SE.Shared.Common.Lookups.WatchListStatus;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class WatchListStatus
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public WatchListStatusEnum Status { get; set; }

    public string WatchListStatusStyle(WatchListStatusEnum watchListStatus)
    {
        switch (watchListStatus)
        {
            case WatchListStatusEnum.Red:
                return "text-danger";

            case WatchListStatusEnum.Yellow:
                return "text-warning";

            default:
                return "text-secondary";
        }
    }
}
