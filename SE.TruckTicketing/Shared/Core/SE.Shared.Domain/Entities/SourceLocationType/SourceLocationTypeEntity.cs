using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.SourceLocationType;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.SourceLocationType, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.SourceLocationType)]
[GenerateManager]
[GenerateProvider]
public class SourceLocationTypeEntity : TTAuditableEntityBase
{
    public SourceLocationTypeCategory Category { get; set; }

    public CountryCode CountryCode { get; set; }

    public DeliveryMethod DefaultDeliveryMethod { get; set; }

    public DownHoleType DefaultDownHoleType { get; set; }

    public string FormatMask { get; set; }

    public string SourceLocationCodeMask { get; set; }

    public string Name { get; set; }

    public bool? EnforceSourceLocationCodeMask { get; set; }

    public bool RequiresApiNumber { get; set; }

    public bool? IsApiNumberVisible { get; set; }

    public bool RequiresCtbNumber { get; set; }

    public bool? IsCtbNumberVisible { get; set; }

    public bool RequiresPlsNumber { get; set; }

    public bool? IsPlsNumberVisible { get; set; }

    public bool RequiresWellFileNumber { get; set; }

    public bool? IsWellFileNumberVisible { get; set; }

    public string ShortFormCode { get; set; }

    public bool IsActive { get; set; }
}
