using System;
using System.Reflection;

using Autofac;

using SE.Integrations.Domain.Processors;

using Trident.Configuration;
using Trident.IoC;

namespace SE.Integrations.Domain;

public class PackageModule : IoCModule
{
    public override void Configure(IIoCProvider builder)
    {
        var targetAssemblies = TargetAssemblies!;

        RegisterDefaultAssemblyScans(builder);
        builder.RegisterAll<ICoreConfiguration>(targetAssemblies);
        builder.UsingTridentSearch(targetAssemblies);
        builder.UsingTridentWorkflowManagers(targetAssemblies);
        builder.UsingTridentValidationManagers(targetAssemblies);
        builder.UsingTridentValidationRules(targetAssemblies);
        builder.UsingTridentWorkflowTasks(targetAssemblies);

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
            const string message = "EntityProcessor is not marked for a target message type.";
            return type.GetCustomAttribute<EntityProcessorForAttribute>()?.EntityType ?? throw new InvalidOperationException(message);
        }
    }
}
