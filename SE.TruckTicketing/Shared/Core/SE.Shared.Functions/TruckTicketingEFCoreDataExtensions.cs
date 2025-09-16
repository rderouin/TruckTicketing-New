using SE.Shared.Domain;

using Trident.EFCore.Contracts;
using Trident.IoC;

namespace SE.Shared.Functions;

public static class TruckTicketingEfCoreDataExtensions
{
    public static IIoCProvider RegisterDynamicFacilityFilterDataContext(this IIoCProvider provider, string connectionStringName = "SecureEnergyDB")
    {
        provider.Register<FacilityQueryFilterContextAccessor, IFacilityQueryFilterContextAccessor>()
                .RegisterNamed<TruckTicketingCosmosDbOptionsBuilder, IOptionsBuilder>(connectionStringName, LifeSpan.SingleInstance)
                .RegisterNamed<TruckTicketingFacilityFilterCosmosDataContext, IEFDbContext>(connectionStringName);

        return provider;
    }
}
