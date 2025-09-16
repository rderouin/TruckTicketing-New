using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

using SE.TruckTicketing.Client.Configuration.Navigation;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Shared;

public partial class TTAppShell : BaseRazorComponent
{
    private NavigationItem _activeHeaderNavigationItem;

    private NavigationItem _activeSideNavigationMenuChildItem;

    private NavigationItem _activeSideNavigationMenuItem;

    private List<NavigationItem> _navigationItems;

    private List<NavigationItem> _subNavigationItems = new();

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private bool DrawerOpen { get; set; } = false;

    private double DrawerWidth { get; set; } = 12.5;

    [CascadingParameter(Name = "UserNavigationContext")]
    public UserNavigationContext UserNavigationContext { get; set; }

    [Inject]
    private SignOutSessionStateManager SignOutManager { get; set; }

    private void ToggleDrawer()
    {
        DrawerOpen = !DrawerOpen;
    }

    public override void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChange;
    }

    protected override void OnParametersSet()
    {
        if (UserNavigationContext == null || _navigationItems != null)
        {
            return;
        }

        SetNavigationItems(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChange;
    }

    private string HeaderUrl(NavigationItem item)
    {
        if (item.Children.Count == 0)
        {
            return item.RelativeUrl;
        }

        var firstChild = item.Children.FirstOrDefault();
        return firstChild.Children.Count == 0 ? firstChild.RelativeUrl : firstChild.Children[0].RelativeUrl;
    }

    private async Task InitiateLogout()
    {
        await SignOutManager.SetSignOutState();
        NavigationManager.NavigateTo("/authentication/logout");
    }

    private void OnLocationChange(object sender, LocationChangedEventArgs e)
    {
        SetNavigationItems(e.Location);
        StateHasChanged();
    }

    private void SetNavigationItems(string location)
    {
        var segments = string.Concat(new Uri(location).Segments);
        _navigationItems = UserNavigationContext?.NavigationItems ?? new();

        _activeHeaderNavigationItem = _navigationItems.FirstOrDefault(item => segments.StartsWith(item.RelativeUrl));
        if (_activeHeaderNavigationItem is null)
        {
            return;
        }

        _subNavigationItems = _activeHeaderNavigationItem.Children;
        _activeSideNavigationMenuChildItem = _subNavigationItems.SelectMany(item => item.Children.Count == 0 ? new() { item } : item.Children)
                                                                .FirstOrDefault(item => segments.StartsWith(item.RelativeUrl));

        if (_activeSideNavigationMenuChildItem is null)
        {
            return;
        }

        if (_activeSideNavigationMenuChildItem.Parent?.Parent != null)
        {
            _activeSideNavigationMenuItem = _activeSideNavigationMenuChildItem.Parent;
        }
    }
}
