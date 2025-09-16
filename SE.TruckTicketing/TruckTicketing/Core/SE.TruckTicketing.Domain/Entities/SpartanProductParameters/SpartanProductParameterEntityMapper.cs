using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;

using Trident.EFCore;

namespace SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

public class SpartanProductParameterEntityMapper : EntityMapper<SpartanProductParameterEntity>
{
    public override void Configure(EntityTypeBuilder<SpartanProductParameterEntity> modelBinding)
    {
        modelBinding.Property(e => e.LocationOperatingStatus)
                    .HasConversion<EnumToStringConverter<LocationOperatingStatus>>();

        modelBinding.Property(e => e.FluidIdentity)
                    .HasConversion<EnumToStringConverter<FluidIdentity>>();
    }
}
