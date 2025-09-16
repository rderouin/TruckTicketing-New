using Microsoft.AspNetCore.Components;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components;

public partial class DropDownLoadingContainer : BaseRazorComponent
{
    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public RenderFragment LoadingView { get; set; }

    [Parameter]
    public RenderFragment LoadedView { get; set; }
}
