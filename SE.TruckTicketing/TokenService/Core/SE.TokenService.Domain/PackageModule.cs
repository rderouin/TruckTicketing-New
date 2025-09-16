using SE.TokenService.Domain.Security;

using Trident.Configuration;
using Trident.IoC;

namespace SE.TokenService.Domain;

public class PackageModule : IoCModule
{
    public override void Configure(IIoCProvider builder)
    {
        RegisterDefaultAssemblyScans(builder);
        builder.RegisterAll<ICoreConfiguration>(TargetAssemblies);
        builder.UsingTridentSearch(TargetAssemblies);
        builder.UsingTridentWorkflowManagers(TargetAssemblies);
        builder.UsingTridentValidationManagers(TargetAssemblies);
        builder.UsingTridentValidationRules(TargetAssemblies);
        builder.UsingTridentWorkflowTasks(TargetAssemblies);

        builder.Register<SecurityClaimsManager, ISecurityClaimsManager>();
    }
}
