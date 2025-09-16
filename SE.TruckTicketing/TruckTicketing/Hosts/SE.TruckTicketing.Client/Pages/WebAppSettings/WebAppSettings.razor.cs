using System.Threading.Tasks;

using Trident.UI.Client;

namespace SE.TruckTicketing.Client.Pages.WebAppSettings;

public partial class WebAppSettings
{
    public static bool CacheEnabled
    {
        get => !HttpServiceBaseBase.DisableCache;
        set => HttpServiceBaseBase.DisableCache = !value;
    }

    protected override Task OnInitializedAsync()
    {
        return base.OnInitializedAsync();
    }
}
