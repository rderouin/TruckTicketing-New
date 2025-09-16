using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.BillingConfiguration;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class BillingConfigurationEntityMap : EntityMapper<BillingConfigurationEntity>, IEntityMapper<BillingConfigurationEntity>
{
    public override void Configure(EntityTypeBuilder<BillingConfigurationEntity> modelBinding)
    {
        modelBinding.Property(x => x.LoadConfirmationFrequency).HasConversion<string>();
        modelBinding.OwnsMany(x => x.MatchCriteria, MatchCriteriaBuilder =>
                                                    {
                                                        MatchCriteriaBuilder.WithOwner();
                                                        MatchCriteriaBuilder.Property(e => e.WellClassification).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.WellClassificationState).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.SourceLocationValueState).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.StreamValueState).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.Stream).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.ServiceTypeValueState).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.SubstanceValueState).HasConversion<string>();
                                                    });
    }
}

public class ApplicantSignatoryMap : EntityMapper<SignatoryContactEntity>, IEntityMapper<SignatoryContactEntity>
{
    public override void Configure(EntityTypeBuilder<SignatoryContactEntity> modelBinding)
    {
    }
}
