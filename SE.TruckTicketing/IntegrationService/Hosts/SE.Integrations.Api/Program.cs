using System;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SE.TridentContrib.Extensions.Azure.Functions;

using Trident.Azure.Functions;
using Trident.IoC;

namespace SE.Integrations.Api;

public static class Program
{
    public static async Task Main()
    {
        await new HostBuilder().UseServiceProviderFactory(new IoCServiceProviderFactory<AutofacIoCProvider>(ConfigureFactory))
                               .ConfigureContainer<IIoCProvider>(builder => { })
                               .ConfigureFunctionsWorkerDefaults(ConfigureWorker)
                               .ConfigureServices(ConfigureServices)
                               .Build()
                               .RunAsync();

        static void ConfigureFactory(IIoCProvider provider)
        {
            provider.RegisterModules(new[]
                     {
                         typeof(PackageModule),
                         typeof(Domain.PackageModule),
                         typeof(Shared.Domain.PackageModule),
                         typeof(Trident.EFCore.PackageModule),
                         typeof(TruckTicketing.Domain.PackageModule),
                     })
                    .RegisterSelf();
        }

        static void ConfigureWorker(IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseMiddleware<ExceptionLoggingMiddleware>();
            builder.UseMiddleware<FunctionContextAccessorMiddleware>();
        }

        static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging();
            services.AddHttpClient("client", client => { client.Timeout = TimeSpan.FromMinutes(15); })
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        }
    }
}
