using Microsoft.AspNetCore.Components;

using Trident.UI.Client.Contracts.Models;

namespace SE.TruckTicketing.Client.Security;

public partial class ClaimsAuthorizeView : ComponentBase
{
    public static readonly string PolicyName = "DefaultSecurityPolicy";

    [Parameter]
    public string ClaimName { get; set; }

    [Parameter]
    public string ClaimValue { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private ResourceClaim Claim => new(ClaimName, ClaimValue);
}
