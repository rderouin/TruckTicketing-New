using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.VolumeChange;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class VolumeChangeEntityMap : EntityMapper<VolumeChangeEntity>, IEntityMapper<VolumeChangeEntity>
{
    public override void Configure(EntityTypeBuilder<VolumeChangeEntity> modelBinding)
    {
        modelBinding.Property(x => x.ProcessOriginal).HasConversion<string>();
        modelBinding.Property(x => x.ProcessAdjusted).HasConversion<string>();
        modelBinding.Property(x => x.VolumeChangeReason).HasConversion<string>();
        modelBinding.Property(x => x.TruckTicketStatus).HasConversion<string>();
    }
}
