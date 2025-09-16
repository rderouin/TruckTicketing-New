using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Security;

public partial class NotAuthorizedView : ComponentBase
{
    [Parameter]
    public RenderFragment Body { get; set; } = default!;
}
