using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.TruckTicket;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class TruckTicketEntityMap : EntityMapper<TruckTicketEntity>
{
    public override void Configure(EntityTypeBuilder<TruckTicketEntity> modelBinding)
    {
        modelBinding.OwnsMany(x => x.AdditionalServices, additionalServicesBuilder =>
                                                         {
                                                             additionalServicesBuilder.WithOwner();
                                                         });

        modelBinding.OwnsMany(a => a.Attachments,
                              subBuilder => subBuilder.Property(a => a.AttachmentType).HasConversion<string>());

        modelBinding.Property(x => x.DowNonDow).HasConversion<string>();
        modelBinding.Property(x => x.WellClassification).HasConversion<string>();
        modelBinding.Property(x => x.Stream).HasConversion<string>();
        modelBinding.Property(x => x.Source).HasConversion<string>();
        modelBinding.Property(x => x.CountryCode).HasConversion<string>();
        modelBinding.Property(x => x.LocationOperatingStatus).HasConversion<string>();
        modelBinding.Property(x => x.Status).HasConversion<string>();
        modelBinding.Property(x => x.ValidationStatus).HasConversion<string>();
        modelBinding.Property(x => x.DowNonDow).HasConversion<string>();
        modelBinding.Property(x => x.FacilityType).HasConversion<string>();
        modelBinding.Property(x => x.VolumeChangeReason).HasConversion<string>();
        modelBinding.Property(x => x.TruckTicketType).HasConversion<string>();
        modelBinding.Property(x => x.CustomerCreditStatus).HasConversion<string>();
        modelBinding.Property(x => x.CustomerWatchListStatus).HasConversion<string>();
        modelBinding.Property(x => x.ReportAsCutType).HasConversion<string>();
        modelBinding.Property(x => x.ServiceTypeClass).HasConversion<string>();
    }
}

public class AdditionalServicesEntityMap : EntityMapper<TruckTicketAdditionalServiceEntity>
{
    public override void Configure(EntityTypeBuilder<TruckTicketAdditionalServiceEntity> modelBinding)
    {
    }
}
