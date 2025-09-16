using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Trident.Data;
using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.EFCore.Contracts;
using Trident.Extensions;

namespace SE.Shared.Functions;

public class TruckTicketingCosmosDbOptionsBuilder : IOptionsBuilder
{
    /// <summary>
    ///     The shared connection string resolver
    /// </summary>
    private readonly ISharedConnectionStringResolver _sharedConnectionStringResolver;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqlServerOptionsBuilder" /> class.
    /// </summary>
    /// <param name="sharedConnectionStringResolver">The shared connection string resolver.</param>
    public TruckTicketingCosmosDbOptionsBuilder(ISharedConnectionStringResolver sharedConnectionStringResolver)
    {
        _sharedConnectionStringResolver = sharedConnectionStringResolver;
    }

    public DbContextOptions GetOptions(string dataSource)
    {
        var conn = new CosmosDbConnection(_sharedConnectionStringResolver.GetConnectionString(dataSource));
        var builder = new DbContextOptionsBuilder<EFCoreCosmosDataContext>()
                     .UseCosmos(conn.AccountEndpoint, conn.AccountKey.ToUnsecureString(), conn.DatabaseName)
                     .ReplaceService<IModelCacheKeyFactory, TruckTicketingFacilityFilterCosmosContextModelCacheKeyFactory>();

        return builder.Options;
    }
}
