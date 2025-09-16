using System.Reflection;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

using SE.TokenService.Api.Configuration;
using SE.TridentContrib.Extensions.Extensions;

using Trident.Azure;
using Trident.Azure.IoC;
using Trident.Contracts.Configuration;
using Trident.EFCore;
using Trident.IoC;
using Trident.Mapper;

namespace SE.TokenService.Api;

public class PackageModule : IoCModule
{
    public override Assembly[] TargetAssemblies => new[] { GetType().Assembly, typeof(Domain.PackageModule).Assembly, typeof(Shared.Domain.PackageModule).Assembly };

    public override void Configure(IIoCProvider builder)
    {
        var config = new FunctionAppSettings();
        RegisterDefaultAssemblyScans(builder);
        builder.RegisterInstance<IAppSettings>(config);
        builder.RegisterInstance(config.ConnectionStrings);
        builder.RegisterDataProviderPackages(TargetAssemblies, config.ConnectionStrings);
        builder.UsingTridentMapperProfiles(TargetAssemblies);
        builder.RegisterAppInsightsLogger();
        builder.RegisterBehavior(() => Options.Create(new WorkerOptions { Serializer = new JsonNetObjectSerializer() }));
        builder.RegisterAzureFunctionTypeFactory(TargetAssemblies);
    }
}
