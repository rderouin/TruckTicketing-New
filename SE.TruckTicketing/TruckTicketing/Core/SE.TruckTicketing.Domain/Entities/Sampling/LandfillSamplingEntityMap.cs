using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Trident.EFCore;

namespace SE.TruckTicketing.Domain.Entities.Sampling;

public class LandfillSamplingEntityMap : EntityMapper<LandfillSamplingEntity>, IEntityMapper<LandfillSamplingEntity>
{
    public override void Configure(EntityTypeBuilder<LandfillSamplingEntity> modelBinding)
    {
        modelBinding.Property(x => x.SamplingRuleType).HasConversion<string>();
    }
}
