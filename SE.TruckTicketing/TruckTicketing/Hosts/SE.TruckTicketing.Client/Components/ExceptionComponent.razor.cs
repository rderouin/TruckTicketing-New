using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SE.TruckTicketing.Client.Components;

public partial class ExceptionComponent : ComponentBase
{
    private bool _isMobile;

    [Inject]
    public IJSRuntime JS { get; set; }

    [Inject]
    public ILogger<ExceptionComponent> Logger { get; set; }

    [Parameter]
    public Exception AppException { get; set; }

    [Parameter]
    public string OriginalUrl { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _isMobile = await JS.InvokeAsync<bool>("isMobileDevice");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "A JavaScript exception occurred calling isMobileDevice");
        }
    }
}
