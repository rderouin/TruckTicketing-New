using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SE.Shared.Functions;
using SE.TridentContrib.Extensions.Azure.Functions;

using Trident.Azure.Functions;
using Trident.IoC;

namespace SE.TruckTicketing.Api;

public static class Program
{
    public static async Task Main()
    {
        await new HostBuilder().UseServiceProviderFactory(new IoCServiceProviderFactory<AutofacIoCProvider>(ConfigureFactory))
                               .ConfigureFunctionsWorkerDefaults(ConfigureWorker)
                               .ConfigureServices(ConfigureServices)
                               .Build()
                               .RunAsync();

        static void ConfigureFactory(IIoCProvider provider)
        {
            provider.RegisterModules(new[] { typeof(Trident.EFCore.PackageModule), typeof(Shared.Domain.PackageModule), typeof(Domain.PackageModule), typeof(PackageModule) })
                    .RegisterDynamicFacilityFilterDataContext()
                    .RegisterSelf();
        }

        static void ConfigureWorker(IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseMiddleware<ExceptionLoggingMiddleware>();
            builder.UseMiddleware<TruckTicketingFunctionSecurityMiddleware>();
            builder.UseMiddleware<FunctionContextAccessorMiddleware>();
        }

        static void ConfigureServices(HostBuilderContext _, IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging();
        }
    }
}
