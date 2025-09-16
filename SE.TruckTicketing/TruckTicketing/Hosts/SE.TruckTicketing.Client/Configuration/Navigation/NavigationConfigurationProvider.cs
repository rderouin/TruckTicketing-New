using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TridentContrib.Extensions.Security;

namespace SE.TruckTicketing.Client.Configuration.Navigation;

public class NavigationConfigurationProvider : INavigationConfigurationProvider
{
    private readonly ITruckTicketingAuthorizationService _authorizationService;

    private readonly HttpClient _httpClient;

    public NavigationConfigurationProvider(NavigationManager navigationManager, IHttpClientFactory httpClientFactory, ITruckTicketingAuthorizationService authorizationService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new(navigationManager.BaseUri);
        _authorizationService = authorizationService;
    }

    public async Task<List<NavigationItem>> GetUserNavigationItems(ClaimsPrincipal user)
    {
        var items = await _httpClient.GetFromJsonAsync<List<NavigationItem>>("navigation.json");
        items?.ForEach(item => SetAuthorization(item, user));
        return items;
    }

    private void SetAuthorization(NavigationItem item, ClaimsPrincipal user)
    {
        var isAnonymous = string.IsNullOrWhiteSpace(item.RequiredResource) || string.IsNullOrWhiteSpace(item.RequiredOperation);
        var isAuthorized = isAnonymous;

        if (item.Children.Count == 0)
        {
            isAuthorized = isAuthorized || _authorizationService.HasPermission(user, item.RequiredResource, item.RequiredOperation);
            item.IsAuthorized = isAuthorized;
            return;
        }

        foreach (var child in item.Children)
        {
            child.Parent = item;
            SetAuthorization(child, user);
        }

        item.Children = item.Children.Where(child => child.IsAuthorized).ToList();

        item.IsAuthorized = item.Children.Count > 0;
    }
}
