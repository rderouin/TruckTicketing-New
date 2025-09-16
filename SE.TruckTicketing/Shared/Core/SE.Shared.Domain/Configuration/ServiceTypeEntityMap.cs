using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.ServiceType;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class ServiceTypeEntityMap : EntityMapper<ServiceTypeEntity>, IEntityMapper<ServiceTypeEntity>
{
    public override void Configure(EntityTypeBuilder<ServiceTypeEntity> modelBinding)
    {
        modelBinding.Property(x => x.CountryCode).HasConversion<string>();
        modelBinding.Property(x => x.Class).HasConversion<string>();
        modelBinding.Property(x => x.TotalThresholdType).HasConversion<string>();
        modelBinding.Property(x => x.TotalFixedUnit).HasConversion<string>();
        modelBinding.Property(x => x.OilThresholdType).HasConversion<string>();
        modelBinding.Property(x => x.OilFixedUnit).HasConversion<string>();
        modelBinding.Property(x => x.WaterThresholdType).HasConversion<string>();
        modelBinding.Property(x => x.WaterFixedUnit).HasConversion<string>();
        modelBinding.Property(x => x.SolidThresholdType).HasConversion<string>();
        modelBinding.Property(x => x.SolidFixedUnit).HasConversion<string>();
        modelBinding.Property(x => x.ReportAsCutType).HasConversion<string>();
        modelBinding.Property(x => x.Stream).HasConversion<string>();
    }
}
