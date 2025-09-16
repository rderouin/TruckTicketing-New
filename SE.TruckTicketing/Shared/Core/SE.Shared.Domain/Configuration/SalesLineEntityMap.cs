using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.SalesLine;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class SalesLineEntityMap : EntityMapper<SalesLineEntity>
{
    public override void Configure(EntityTypeBuilder<SalesLineEntity> modelBinding)
    {
        modelBinding.Property(x => x.WellClassification).HasConversion<string>();
        modelBinding.Property(x => x.Status).HasConversion<string>();
        modelBinding.Property(x => x.DowNonDow).HasConversion<string>();
        modelBinding.Property(x => x.AttachmentIndicatorType).HasConversion<string>();
        modelBinding.Property(x => x.HasAttachments).HasConversion<string>();
        modelBinding.Property(x => x.ChangeReason).HasConversion<string>();
        modelBinding.Property(x => x.CutType).HasConversion<string>();
    }
}
