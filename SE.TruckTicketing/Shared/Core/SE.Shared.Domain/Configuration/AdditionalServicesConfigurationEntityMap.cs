using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class AdditionalServicesConfigurationEntityMap : EntityMapper<AdditionalServicesConfigurationEntity>, IEntityMapper<AdditionalServicesConfigurationEntity>
{
    public override void Configure(EntityTypeBuilder<AdditionalServicesConfigurationEntity> modelBinding)
    {
        modelBinding.OwnsMany(x => x.MatchCriteria, MatchCriteriaBuilder =>
                                                    {
                                                        MatchCriteriaBuilder.WithOwner();
                                                        MatchCriteriaBuilder.Property(x => x.WellClassificationState).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.WellClassification).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.SourceIdentifierValueState).HasConversion<string>();
                                                        MatchCriteriaBuilder.Property(e => e.SubstanceValueState).HasConversion<string>();
                                                    });

        modelBinding.Property(x => x.FacilityType).HasConversion<string>();
    }
}

public class MatchPredicateEntityMap : EntityMapper<AdditionalServicesConfigurationMatchPredicateEntity>, IEntityMapper<AdditionalServicesConfigurationMatchPredicateEntity>
{
    public override void Configure(EntityTypeBuilder<AdditionalServicesConfigurationMatchPredicateEntity> modelBinding)
    {
    }
}

public class AdditionalServicesConfigurationProductMap : EntityMapper<AdditionalServicesConfigurationAdditionalServiceEntity>, IEntityMapper<AdditionalServicesConfigurationAdditionalServiceEntity>
{
    public override void Configure(EntityTypeBuilder<AdditionalServicesConfigurationAdditionalServiceEntity> modelBinding)
    {
    }
}
