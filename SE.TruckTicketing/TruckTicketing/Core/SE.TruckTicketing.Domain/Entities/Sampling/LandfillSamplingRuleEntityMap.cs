using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Trident.EFCore;

namespace SE.TruckTicketing.Domain.Entities.Sampling;

public class LandfillSamplingRuleEntityMap : EntityMapper<LandfillSamplingRuleEntity>, IEntityMapper<LandfillSamplingRuleEntity>
{
    public override void Configure(EntityTypeBuilder<LandfillSamplingRuleEntity> modelBinding)
    {
        modelBinding.Property(x => x.SamplingRuleType).HasConversion<string>();
        modelBinding.Property(x => x.Province).HasConversion<string>();
        modelBinding.Property(x => x.CountryCode).HasConversion<string>();
    }
}
