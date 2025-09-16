using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.Invoices;

using Trident.EFCore;

namespace SE.Integrations.Api.Configuration;

public class InvoiceEntityMap : EntityMapper<InvoiceEntity>, IEntityMapper<InvoiceEntity>
{
    public override void Configure(EntityTypeBuilder<InvoiceEntity> modelBinding)
    {
        modelBinding.Property(e => e.Status).HasConversion<string>();
        modelBinding.Property(e => e.DowNonDow).HasConversion<string>();
        modelBinding.Property(e => e.AttachmentIndicatorType).HasConversion<string>();
        modelBinding.Property(e => e.DistributionMethod).HasConversion<string>();
        modelBinding.Property(e => e.CollectionOwner).HasConversion<string>();
        modelBinding.Property(e => e.CollectionReason).HasConversion<string>();
        modelBinding.Property(e => e.InvoiceReversalReason).HasConversion<string>();
        modelBinding.Property(x => x.HasAttachments).HasConversion<string>();
        modelBinding.Property(x => x.CustomerWatchListStatus).HasConversion<string>();
        modelBinding.Property(x => x.CustomerCreditStatus).HasConversion<string>();
    }
}
