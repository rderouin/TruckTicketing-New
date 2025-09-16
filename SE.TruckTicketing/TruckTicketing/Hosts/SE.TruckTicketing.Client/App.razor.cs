using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace SE.TruckTicketing.Client;

public partial class App
{
    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Inject]
    private ILogger<App> Logger { get; set; }

    private Task RouterNavigating(NavigationContext navigationContext)
    {
        Logger.LogInformation(navigationContext.Path);
        return Task.CompletedTask;
    }
}
