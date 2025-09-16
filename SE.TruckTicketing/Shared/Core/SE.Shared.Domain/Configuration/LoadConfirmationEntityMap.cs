using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.LoadConfirmation;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class LoadConfirmationEntityMap : EntityMapper<LoadConfirmationEntity>, IEntityMapper<LoadConfirmationEntity>
{
    public override void Configure(EntityTypeBuilder<LoadConfirmationEntity> modelBinding)
    {
        modelBinding.Property(x => x.CustomerWatchListStatus).HasConversion<string>();
        modelBinding.Property(x => x.CustomerCreditStatus).HasConversion<string>();
        modelBinding.Property(e => e.Status).HasConversion<string>();
        modelBinding.Property(e => e.InvoiceStatus).HasConversion<string>();
    }
}
