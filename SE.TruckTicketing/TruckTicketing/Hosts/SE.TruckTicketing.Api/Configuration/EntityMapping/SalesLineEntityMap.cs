using System.Linq;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.SalesLine;

using Trident.EFCore;

namespace SE.TruckTicketing.Api.Configuration.EntityMapping;

public class SalesLineEntityMap : EntityMapper<SalesLineEntity>
{
    private readonly IFacilityQueryFilterContextAccessor _queryFilterContextAccessor;

    public SalesLineEntityMap(IFacilityQueryFilterContextAccessor queryFilterContextAccessor)
    {
        _queryFilterContextAccessor = queryFilterContextAccessor;
    }

    public override void Configure(EntityTypeBuilder<SalesLineEntity> modelBinding)
    {
        modelBinding.Property(x => x.WellClassification).HasConversion<string>();
        modelBinding.Property(x => x.Status).HasConversion<string>();
        modelBinding.Property(x => x.DowNonDow).HasConversion<string>();
        modelBinding.Property(x => x.AttachmentIndicatorType).HasConversion<string>();
        modelBinding.Property(x => x.HasAttachments).HasConversion<string>();
        modelBinding.Property(x => x.ChangeReason).HasConversion<string>();
        modelBinding.Property(x => x.CutType).HasConversion<string>();

        var queryFilterContext = _queryFilterContextAccessor.FacilityQueryFilterContext;
        if (queryFilterContext is not null && queryFilterContext.AllowedFacilityIds.Any())
        {
            modelBinding.HasQueryFilter(salesLine => queryFilterContext.AllowedFacilityIds.Contains(salesLine.FacilityId));
        }
    }
}
