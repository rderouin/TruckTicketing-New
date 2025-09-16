using SE.Shared.Common.Enums;
using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.EDIFieldLookup;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), nameof(EDIFieldLookupEntity), PartitionKeyType.WellKnown)]
[Discriminator(Containers.Discriminators.EDIFieldLookup, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
public class EDIFieldLookupEntity : TTEntityBase
{
    public string Name { get; set; }

    public DataTypes DataType { get; set; }
}
