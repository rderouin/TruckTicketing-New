using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SE.TruckTicketing.Client.Configuration.Navigation;

public interface INavigationConfigurationProvider
{
    Task<List<NavigationItem>> GetUserNavigationItems(ClaimsPrincipal user);
}
