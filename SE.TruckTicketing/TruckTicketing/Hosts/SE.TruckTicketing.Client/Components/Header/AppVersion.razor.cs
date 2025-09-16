using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Trident.Contracts.Configuration;

namespace SE.TruckTicketing.Client.Components.Header;

public partial class AppVersion
{
    private const string Version = nameof(Version);

    private const string Environment = nameof(Environment);

    [Inject]
    public IAppSettings AppSettings { get; set; }

    public string CurrentVersion { get; set; }

    public string CurrentEnvironment { get; set; }

    protected override Task OnInitializedAsync()
    {
        CurrentVersion = AppSettings[Version] ?? "-";
        CurrentEnvironment = AppSettings[Environment] ?? "-";

        return base.OnInitializedAsync();
    }
}
