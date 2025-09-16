using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using SE.Shared.Domain.Entities.SourceLocation;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class SourceLocationEntityMapper : EntityMapper<SourceLocationEntity>
{
    public override void Configure(EntityTypeBuilder<SourceLocationEntity> modelBinding)
    {
        modelBinding.Property(e => e.CountryCode)
                    .HasConversion<EnumToStringConverter<CountryCode>>();

        modelBinding.Property(e => e.SourceLocationTypeCategory)
                    .HasConversion<EnumToStringConverter<SourceLocationTypeCategory>>();

        modelBinding.Property(e => e.DownHoleType)
                    .HasConversion<EnumToStringConverter<DownHoleType>>();

        modelBinding.Property(e => e.DeliveryMethod)
                    .HasConversion<EnumToStringConverter<DeliveryMethod>>();

        modelBinding.Property(e => e.ProvinceOrState)
                    .HasConversion<EnumToStringConverter<StateProvince>>();
    }
}
