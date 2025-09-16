using System;

using SE.Shared.Domain;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;

using Trident.Data;

namespace SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Products, nameof(DocumentType), nameof(SpartanProductParameterEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.SpartanProductParameter)]
public class SpartanProductParameterEntity : TTAuditableEntityBase
{
    public string ProductName { get; set; }

    public FluidIdentity FluidIdentity { get; set; }

    public double MinFluidDensity { get; set; }

    public double MaxFluidDensity { get; set; }

    public double MinWaterPercentage { get; set; }

    public double MaxWaterPercentage { get; set; }

    public bool ShowDensity { get; set; }

    public LocationOperatingStatus LocationOperatingStatus { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsActive { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntity { get; set; }
}
