using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SE.Shared.Functions;

public class TruckTicketingFacilityFilterCosmosContextModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        return context is TruckTicketingFacilityFilterCosmosDataContext dynamicContext
                   ? (context.GetType(), dynamicContext.FacilityFilterCacheKey, designTime)
                   : context.GetType();
    }

    public object Create(DbContext context)
    {
        return Create(context, false);
    }
}
