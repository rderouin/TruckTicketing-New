using System;
using System.Reflection;

using Autofac;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Processors;
using SE.Shared.Functions;
using SE.TridentContrib.Extensions.Extensions;
using SE.TruckTicketing.Api.Configuration;

using Trident.Azure;
using Trident.Azure.IoC;
using Trident.Contracts.Configuration;
using Trident.EFCore;
using Trident.IoC;
using Trident.Mapper;

namespace SE.TruckTicketing.Api;

public class PackageModule : IoCModule
{
    public override Assembly[] TargetAssemblies => new[] { GetType().Assembly, typeof(Shared.Domain.PackageModule).Assembly, typeof(Domain.PackageModule).Assembly };

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
        builder.RegisterTruckTicketingFunctionMiddlewareDependencies();
        builder.RegisterEmailProcessors(TargetAssemblies);
        RegisterEntityProcessors(builder);
    }

    private void RegisterEntityProcessors(IIoCProvider builder)
    {
        var autofacBuilder = (AutofacIoCProvider)builder;
        var targetAssemblies = TargetAssemblies!;

        autofacBuilder.Builder!
                      .RegisterAssemblyTypes(targetAssemblies)
                      .Where(t => t.IsAssignableTo<IEntityProcessor>())
                      .Named<IEntityProcessor>(GetEntityProcessorKey)
                      .AsImplementedInterfaces()
                      .InstancePerLifetimeScope()
                      .AsSelf();

        static string GetEntityProcessorKey(Type type)
        {
            var message = $"EntityProcessor is not marked for a target message type '{type.FullName}'.";
            return type.GetCustomAttribute<EntityProcessorForAttribute>()?.EntityType ??
                   throw new InvalidOperationException(message);
        }
    }
}
