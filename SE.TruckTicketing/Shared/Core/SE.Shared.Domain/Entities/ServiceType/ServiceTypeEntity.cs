using System;

using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;

namespace SE.Shared.Domain.Entities.ServiceType;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.ServiceType, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.ServiceType)]
public class ServiceTypeEntity : TTEntityBase, ITTSearchableIdBase
{
    public string Description { get; set; }

    public CountryCode CountryCode { get; set; }

    public string CountryCodeString { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntityCode { get; set; }

    public string ServiceTypeId { get; set; }

    public string Name { get; set; }

    public string Hash { get; set; }

    public Class Class { get; set; }

    public Guid TotalItemId { get; set; }

    public string TotalItemName { get; set; }

    public SubstanceThresholdType TotalThresholdType { get; set; }

    public bool TotalShowZeroDollarLine { get; set; }

    public SubstanceThresholdFixedUnit TotalFixedUnit { get; set; }

    public double? TotalMinValue { get; set; }

    public double? TotalMaxValue { get; set; }

    public Guid OilItemId { get; set; }

    public string OilItemName { get; set; }

    public bool OilItemReverse { get; set; }

    public bool OilShowZeroDollarLine { get; set; }

    public double OilCreditMinVolume { get; set; }

    public SubstanceThresholdType OilThresholdType { get; set; }

    public SubstanceThresholdFixedUnit OilFixedUnit { get; set; }

    public double? OilMinValue { get; set; }

    public double? OilMaxValue { get; set; }

    public Guid WaterItemId { get; set; }

    public string WaterItemName { get; set; }

    public bool WaterShowZeroDollarLine { get; set; }

    public double? WaterMinPricingPercentage { get; set; }

    public double? WaterMinValue { get; set; }

    public SubstanceThresholdType WaterThresholdType { get; set; }

    public SubstanceThresholdFixedUnit WaterFixedUnit { get; set; }

    public double? WaterMaxValue { get; set; }

    public Guid SolidItemId { get; set; }

    public string SolidItemName { get; set; }

    public double? SolidMinPricingPercentage { get; set; }

    public bool SolidShowZeroDollarLine { get; set; }

    public double? SolidMinValue { get; set; }

    public SubstanceThresholdType SolidThresholdType { get; set; }

    public SubstanceThresholdFixedUnit SolidFixedUnit { get; set; }

    public double? SolidMaxValue { get; set; }

    public ReportAsCutTypes ReportAsCutType { get; set; }

    public Stream Stream { get; set; }

    public bool ProrateService { get; set; }

    public bool ProductionAccountantReport { get; set; }

    public bool IncludesWater { get; set; }

    public bool IncludesOil { get; set; }

    public bool IncludesSolids { get; set; }

    public bool IsActive { get; set; } = true;

    public string SearchableId { get; set; }
}

public class ServiceTypeReleasedProductLookup : OwnedLookupEntityBase<Guid>
{
    public Guid ReleasedProductId { get; set; }

    public string ReleasedProductDisplayValue { get; set; }
}
