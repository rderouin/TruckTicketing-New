using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Trident.EFCore;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

public class InvoiceExchangeEntityMapper : EntityMapper<InvoiceExchangeEntity>
{
    public override void Configure(EntityTypeBuilder<InvoiceExchangeEntity> modelBinding)
    {
        modelBinding.Property(e1 => e1.Type).HasConversion<string>();
        
        modelBinding.OwnsOne(e1 => e1.InvoiceDeliveryConfiguration,
                             b1 =>
                             {
                                 b1.Property(e2 => e2.MessageAdapterType).HasConversion<string>();
                                 b1.Property(e2 => e2.PollingStrategy).HasConversion<string>();
                                 b1.OwnsOne(e2 => e2.TransportSettings, b2 =>
                                                                        {
                                                                            b2.Property(e3 => e3.TransportType).HasConversion<string>();
                                                                            b2.Property(e3 => e3.HttpVerb).HasConversion<string>();
                                                                        });
                             });

        modelBinding.OwnsOne(e1 => e1.FieldTicketsDeliveryConfiguration,
                             b1 =>
                             {
                                 b1.Property(e2 => e2.MessageAdapterType).HasConversion<string>();
                                 b1.Property(e2 => e2.PollingStrategy).HasConversion<string>();
                                 b1.OwnsOne(e2 => e2.TransportSettings, b2 =>
                                                                        {
                                                                            b2.Property(e3 => e3.TransportType).HasConversion<string>();
                                                                            b2.Property(e3 => e3.HttpVerb).HasConversion<string>();
                                                                        });
                             });
    }
}
