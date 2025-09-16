using SE.Shared.Domain;
using SE.TridentContrib.Extensions.Security;

using Trident.Caching;
using Trident.Common;
using Trident.IoC;
using Trident.Security;

namespace SE.Shared.Functions;

public static class PackageModuleExtensions
{
    public static IIoCProvider RegisterTruckTicketingFunctionMiddlewareDependencies(this IIoCProvider builder)
    {
        builder.Register<InMemoryCachingManager, ICachingManager>();
        builder.Register<TruckTicketingAuthorizationService, IAuthorizationService>();
        builder.Register<TruckTicketingAuthorizationService, ITruckTicketingAuthorizationService>(LifeSpan.SingleInstance);
        builder.Register<UserContextAccessor, IUserContextAccessor>();
        builder.Register<FacilityQueryFilterContextAccessor, IFacilityQueryFilterContextAccessor>();
        builder.Register<EntityComparer, IEntityComparer>();

        return builder;
    }
}
