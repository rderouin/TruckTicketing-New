using System.Net.Http;
using System.Threading.Tasks;

using BlazorApplicationInsights;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Radzen;

using SE.TruckTicketing.Client.Configuration;

using Toolbelt.Blazor.Extensions.DependencyInjection;

using Trident.Contracts.Configuration;
using Trident.IoC;
using Trident.UI.Blazor.Logging;
using Trident.UI.Blazor.Logging.AppInsights;

namespace SE.TruckTicketing.Client;

public class Program
{
    private static async Task Main(params string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        await ConfigureSourceLocationSettings(builder);
        await ConfigureAppVersion(builder);
        await ConfigureChangeSettings(builder);
        var appSettings = new AppSettings(builder.Configuration);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");
        builder.ConfigureSecurity();
        builder.ConfigureHttpClients();
        builder.ConfigureContainer(new IoCServiceProviderFactory<AutofacIoCProvider>(provider =>
                                                                                     {
                                                                                         provider.RegisterInstance<IAppSettings>(appSettings);
                                                                                         provider.Populate(builder.Services);
                                                                                         provider.RegisterModules(new[]
                                                                                                  {
                                                                                                      typeof(Trident.UI.Blazor.PackageModule),
                                                                                                      typeof(SE.TruckTicketing.UI.PackageModule),
                                                                                                      typeof(SE.TruckTicketing.Client.PackageModule),
                                                                                                  })
                                                                                                 .RegisterSelf();
                                                                                     }));

        builder.Services.AddHotKeys();
        builder.Services.AddBlazorDownloadFile();
        builder.Services.AddScoped<TooltipService>();
        builder.Logging.AddBlazorClientLogging(new LoggingConfiguration
                                               {
                                                   LogLevel = LogLevel.Trace,
                                               },
                                               new AppInsightsConfig
                                               {
                                                   ConnectionString = appSettings["ApplicationInsights:ConnectionString"] ?? "",
                                                   InstrumentationKey = appSettings["ApplicationInsights:InstrumentationKey"] ?? "",
                                                   DisableFetchTracking = false,
                                                   EnableCorsCorrelation = true,
                                                   EnableRequestHeaderTracking = true,
                                                   EnableResponseHeaderTracking = true,
                                               },
                                               false);
        builder.Services.AddBlazorApplicationInsights();
        var host = builder.Build();
        await host.RunAsync();
    }

    private static async Task ConfigureSourceLocationSettings(WebAssemblyHostBuilder builder)
    {
        await AddJsonStream(builder, "source-location-settings.json", true);
    }

    private static async Task ConfigureAppVersion(WebAssemblyHostBuilder builder)
    {
        await AddJsonStream(builder, "app-version.json", false);
    }

    private static async Task ConfigureChangeSettings(WebAssemblyHostBuilder builder)
    {
        await AddJsonStream(builder, "change.settings.json", false);
    }

    private static async Task AddJsonStream(WebAssemblyHostBuilder builder, string jsonFile, bool addService)
    {
        var http = new HttpClient
        {
            BaseAddress = new(builder.HostEnvironment.BaseAddress),
        };

        if (addService)
        {
            builder.Services.AddScoped(sp => http);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, jsonFile);
        request.SetBrowserRequestCache(BrowserRequestCache.NoStore);
        using var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            builder.Configuration.AddJsonStream(stream);
        }
    }
}
