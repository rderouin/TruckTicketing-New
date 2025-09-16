using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.EDIFieldLookup;

using Trident.EFCore;

namespace SE.TruckTicketing.Api.Configuration.EntityMapping;

public class EDIEntityMapping : EntityMapper<EDIFieldLookupEntity>, IEntityMapper<EDIFieldLookupEntity>
{
    public override void Configure(EntityTypeBuilder<EDIFieldLookupEntity> modelBinding)
    {
        modelBinding.Property(x => x.DataType).HasConversion<string>();
    }
}
