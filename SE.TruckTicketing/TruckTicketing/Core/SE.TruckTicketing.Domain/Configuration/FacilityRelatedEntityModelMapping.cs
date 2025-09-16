using System.Linq;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities;

using Trident.EFCore;

namespace SE.TruckTicketing.Domain.Configuration;

public class FacilityRelatedEntityModelMapping : EntityMapper<IFacilityRelatedEntity>, IEntityMapper<IFacilityRelatedEntity>
{
    private readonly IFacilityQueryFilterContextAccessor _queryFilterContextAccessor;

    public FacilityRelatedEntityModelMapping(IFacilityQueryFilterContextAccessor queryFilterContextAccessor)
    {
        _queryFilterContextAccessor = queryFilterContextAccessor;
    }

    public override void Configure(EntityTypeBuilder<IFacilityRelatedEntity> modelBinding)
    {
        var queryFilterContext = _queryFilterContextAccessor.FacilityQueryFilterContext;
        if (queryFilterContext is not null && queryFilterContext.AllowedFacilityIds.Any())
        {
            modelBinding.HasQueryFilter(entity => queryFilterContext.AllowedFacilityIds.Contains(entity.FacilityId));
        }
    }
}
