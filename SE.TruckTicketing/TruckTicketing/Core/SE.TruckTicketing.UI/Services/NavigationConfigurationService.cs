using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Navigation;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.navigation)]
public class NavigationConfigurationService : ServiceBase<NavigationConfigurationService, NavigationModel, Guid>, INavigationConfigurationService
{
    //TODO: Link to Arinze's service
    //private readonly IAuthorizationService _authorizationService;
    public NavigationConfigurationService(ILogger<NavigationConfigurationService> logger,
                                          IHttpClientFactory httpClientFactory
        //,IAuthorizationService authorizationService
    )
        : base(logger, httpClientFactory)
    {
        //_authorizationService = authorizationService; 
    }

    public async Task<NavigationModel> GetAuthFilteredNavigationConfiguration(string profileName, ClaimsPrincipal principal)
    {
        var results = await Search(new() { Filters = new() { { nameof(NavigationModel.ProfileName), profileName } } });

        var navConfig = results?.Results == null ? new() : results?.Results?.FirstOrDefault();
        var authItems = new List<NavigationItemModel>();
        if (navConfig != null && navConfig.NavigationItems != null)
        {
            foreach (var item in navConfig.NavigationItems)
            {
                authItems.Add(item);
                //TODO: Hookup to Arinze's Auth Service
                //if (_authorizationService.HasPermission(principal, item.ClaimType, item.ClaimValue))
                //{
                //    authItems.Add(item);
                //}
            }
        }

        navConfig.NavigationItems = authItems;

        return navConfig;
    }
}
