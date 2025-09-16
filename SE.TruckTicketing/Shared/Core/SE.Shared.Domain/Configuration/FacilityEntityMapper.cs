using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.Facilities;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class FacilityEntityMapper : EntityMapper<FacilityEntity>
{
    public override void Configure(EntityTypeBuilder<FacilityEntity> modelBinding)
    {
        modelBinding.Property(e => e.CountryCode).HasConversion<string>();
        modelBinding.Property(e => e.Type).HasConversion<string>();
        modelBinding.Property(x => x.Province).HasConversion<string>();
    }
}
