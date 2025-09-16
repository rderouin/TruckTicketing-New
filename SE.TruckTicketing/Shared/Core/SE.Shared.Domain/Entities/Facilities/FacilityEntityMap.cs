using System.Linq;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Trident.EFCore;

namespace SE.Shared.Domain.Entities.Facilities;

public class FacilityEntityMap : EntityMapper<FacilityEntity>
{
    private readonly IFacilityQueryFilterContextAccessor _queryFilterContextAccessor;

    public FacilityEntityMap(IFacilityQueryFilterContextAccessor queryFilterContextAccessor)
    {
        _queryFilterContextAccessor = queryFilterContextAccessor;
    }

    public override void Configure(EntityTypeBuilder<FacilityEntity> modelBinding)
    {
        modelBinding.Property(x => x.Province).HasConversion<string>();
        
        var queryFilterContext = _queryFilterContextAccessor.FacilityQueryFilterContext;
        if (queryFilterContext is not null && queryFilterContext.AllowedFacilityIds.Any())
        {   
            modelBinding.HasQueryFilter(facility => queryFilterContext.AllowedFacilityIds.Contains(facility.Id));
        }
    }
}
