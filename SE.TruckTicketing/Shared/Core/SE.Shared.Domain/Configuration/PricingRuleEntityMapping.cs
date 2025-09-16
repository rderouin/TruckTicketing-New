using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.PricingRules;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class PricingRuleEntityMapping : EntityMapper<PricingRuleEntity>, IEntityMapper<PricingRuleEntity>
{
    public override void Configure(EntityTypeBuilder<PricingRuleEntity> modelBinding)
    {
        modelBinding.Property(x => x.SalesQuoteType).HasConversion<string>();
    }
}
