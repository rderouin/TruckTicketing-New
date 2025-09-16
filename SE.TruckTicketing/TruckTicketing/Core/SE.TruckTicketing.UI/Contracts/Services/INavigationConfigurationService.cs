using System;
using System.Security.Claims;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Navigation;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface INavigationConfigurationService : IServiceBase<NavigationModel, Guid>
{
    Task<NavigationModel> GetAuthFilteredNavigationConfiguration(string profileName, ClaimsPrincipal principal);
}
