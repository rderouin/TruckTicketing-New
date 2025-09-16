using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.EDIFieldLookup;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class EDIFieldLookupEntityMap : EntityMapper<EDIFieldLookupEntity>, IEntityMapper<EDIFieldLookupEntity>
{
    public override void Configure(EntityTypeBuilder<EDIFieldLookupEntity> modelBinding)
    {
        modelBinding.Property(x => x.DataType).HasConversion<string>();
    }
}
