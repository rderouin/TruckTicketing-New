using System;

using SE.Shared.Domain;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.FacilityService;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.FacilityService, PartitionKeyType.WellKnown)]
[Discriminator(Databases.Discriminators.FacilityServiceSubstanceIndex, Property = nameof(EntityType))]
[GenerateProvider]
public class FacilityServiceSubstanceIndexEntity : TTEntityBase
{
    public const int LatestIndexVersion = 2;

    public Guid FacilityId { get; set; }

    public Guid FacilityServiceId { get; set; }

    public string FacilityServiceNumber { get; set; }

    public Guid ServiceTypeId { get; set; }

    public string ServiceTypeName { get; set; }

    public Guid TotalProductId { get; set; }

    public string TotalProductName { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> TotalProductCategories { get; set; }

    public string Substance { get; set; }

    public Guid SubstanceId { get; set; }

    public string WasteCode { get; set; }

    public string Stream { get; set; }

    public string UnitOfMeasure { get; set; }

    public bool IsAuthorized { get; set; }

    public int? IndexVersion { get; set; }

    public bool IsLatestVersion()
    {
        return IndexVersion == LatestIndexVersion;
    }
}
