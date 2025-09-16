using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Trident.Azure.Functions;
using Trident.IoC;

namespace SE.TokenService.Api;

public class Program
{
    public static async Task Main()
    {
        var hostBuilder = new HostBuilder();

        var host = hostBuilder.UseServiceProviderFactory(new IoCServiceProviderFactory<AutofacIoCProvider>(provider =>
                                                                                                           {
                                                                                                               provider.RegisterModules(new[]
                                                                                                                        {
                                                                                                                            typeof(PackageModule),
                                                                                                                            typeof(Shared.Domain.PackageModule),
                                                                                                                            typeof(Domain.PackageModule),
                                                                                                                            typeof(Trident.EFCore.PackageModule),
                                                                                                                        })
                                                                                                                       .RegisterSelf();
                                                                                                           }))
                              .ConfigureFunctionsWorkerDefaults(builder =>
                                                                {
                                                                    builder.UseMiddleware<ExceptionLoggingMiddleware>();
                                                                    //builder.UseMiddleware<FunctionsSecurityMiddleware>();
                                                                }).Build();

        await host.RunAsync();
    }
}
