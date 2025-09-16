using System.Reflection;

using Trident.Contracts.Api.Client;
using Trident.IoC;

namespace SE.TruckTicketing.UI;

public class PackageModule : IoCModule
{
    public override Assembly[] TargetAssemblies => new[] { GetType().Assembly, typeof(Trident.UI.Client.PackageModule).Assembly };

    public override void Configure(IIoCProvider builder)
    {
        builder.RegisterAll<IServiceProxy>(TargetAssemblies);
    }
}
