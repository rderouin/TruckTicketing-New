using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.LegalEntity;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class LegalEntityEntityMapping : EntityMapper<LegalEntityEntity>, IEntityMapper<LegalEntityEntity>
{
    public override void Configure(EntityTypeBuilder<LegalEntityEntity> modelBinding)
    {
        modelBinding.Property(x => x.CountryCode).HasConversion<string>();
    }
}
