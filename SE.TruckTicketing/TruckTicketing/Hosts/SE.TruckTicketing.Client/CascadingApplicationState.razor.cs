using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Configuration.Navigation;
using SE.TruckTicketing.Contracts.Security;

using Trident.UI.Blazor.Logging.AppInsights;
using Trident.UI.Blazor.Models;

namespace SE.TruckTicketing.Client;

public partial class CascadingApplicationState : ComponentBase, IDisposable
{
    private Task<AuthenticationState> _currentAuthenticationStateTask;

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Inject]
    private INavigationConfigurationProvider NavigationConfigurationProvider { get; set; }

    private UserNavigationContext UserNavigationContext { get; set; }

    [Inject]
    private IApplicationInsights ApplicationInsights { get; set; }

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; }

    private ApplicationContext ApplicationContext { get; set; } = new();

    private NavigationHistoryManager NavigationHistoryManager { get; set; } = new();

    void IDisposable.Dispose()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= AuthenticationStateChanged;
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        _currentAuthenticationStateTask ??= AuthenticationStateProvider.GetAuthenticationStateAsync();

        var state = await _currentAuthenticationStateTask;
        var authenticated = state?.User?.Identity?.IsAuthenticated;
        if (authenticated == true)
        {
            await RefreshUserContext();
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            AuthenticationStateProvider.AuthenticationStateChanged += AuthenticationStateChanged;

            if (!NavigationManager.Uri.Contains("authentication/login-callback") &&
                !NavigationManager.Uri.Contains("authentication/logout-callback") &&
                !NavigationManager.Uri.Contains("authentication/login") &&
                NavigationManager.ToBaseRelativePath(NavigationManager.Uri) != "authentication/logout" &&
                NavigationManager.ToBaseRelativePath(NavigationManager.Uri) != "authentication/logged-out")
            {
                Navigation.NavigateTo($"authentication/login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}");
            }
        }

        NavigationManager.LocationChanged += HandleLocationChanged;

        await base.OnInitializedAsync();
    }

    private void HandleLocationChanged(object sender, LocationChangedEventArgs @event)
    {
        NavigationHistoryManager.AddLocation(@event.Location);
    }

    private async void AuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        _currentAuthenticationStateTask = authenticationStateTask;
        var state = await _currentAuthenticationStateTask;

        if (state?.User.Identity?.IsAuthenticated == true)
        {
            await RefreshUserContext();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshUserContext()
    {
        var state = await _currentAuthenticationStateTask;
        if (state?.User.Identity?.IsAuthenticated ?? false)
        {
            var acctIdClaim = state.User.Claims.First(x => x.Type == "sub");

            if (Guid.TryParse(acctIdClaim.Value, out var acctId))
            {
                if (ApplicationContext.User?.UserId != acctId)
                {
                    // AI
                    await ApplicationInsights.SetAuthenticatedUserContext(acctIdClaim.Value);
                    SetUserInfoTelemetry(state.User);

                    // context
                    ApplicationContext.User = new(acctId, state.User);
                    ApplicationContext.Lookups = new();

                    // nav
                    UserNavigationContext = new(await NavigationConfigurationProvider.GetUserNavigationItems(state.User));
                }
            }
        }
        else
        {
            // clear AI
            await ApplicationInsights.ClearAuthenticatedUserContext();
            SetUserInfoTelemetry(null);
        }
    }

    private void SetUserInfoTelemetry(ClaimsPrincipal principal)
    {
        ApplicationInsights.AddTelemetryInitializer(new()
        {
            Data = new()
            {
                ["ttUserFullName"] = TryGetClaim(ClaimConstants.Name),
                ["ttUserEmails"] = TryGetClaim(ClaimConstants.Emails),
                ["ttFacilityAccess"] = TryDecodeBase64(TryGetClaim("extension_TTFacilityAccess")),
                ["ttPermissions"] = TryDecodeBase64(TryGetClaim("extension_TTPermissions")),
            },
        });

        string TryGetClaim(string type)
        {
            if (principal?.Claims.FirstOrDefault(c => c.Type == type) is { } claim)
            {
                return claim.Value;
            }

            return null;
        }

        string TryDecodeBase64(string base64)
        {
            try
            {
                if (!base64.HasText())
                {
                    return null;
                }

                var data = Convert.FromBase64String(base64);
                var str = Encoding.UTF8.GetString(data);
                return str;
            }
            catch
            {
                return null;
            }
        }
    }
}
