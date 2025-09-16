using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class SourceLocationTypeEntityMapper : EntityMapper<SourceLocationTypeEntity>
{
    public override void Configure(EntityTypeBuilder<SourceLocationTypeEntity> modelBinding)
    {
        modelBinding.Property(e => e.Category)
                    .HasConversion<EnumToStringConverter<SourceLocationTypeCategory>>();

        modelBinding.Property(e => e.CountryCode)
                    .HasConversion<EnumToStringConverter<CountryCode>>();

        modelBinding.Property(e => e.DefaultDeliveryMethod)
                    .HasConversion<EnumToStringConverter<DeliveryMethod>>();

        modelBinding.Property(e => e.DefaultDownHoleType)
                    .HasConversion<EnumToStringConverter<DownHoleType>>();
    }
}
