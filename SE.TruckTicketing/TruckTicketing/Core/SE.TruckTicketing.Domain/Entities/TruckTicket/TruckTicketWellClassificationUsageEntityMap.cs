using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Trident.EFCore;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class TruckTicketWellClassificationUsageEntityMap : EntityMapper<TruckTicketWellClassificationUsageEntity>, IEntityMapper<TruckTicketWellClassificationUsageEntity>
{
    public override void Configure(EntityTypeBuilder<TruckTicketWellClassificationUsageEntity> modelBinding)
    {
        modelBinding.Property(x => x.WellClassification).HasConversion<string>();
    }
}
