using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.BusinessStream;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(BusinessStreamEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.BusinessStream)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class BusinessStreamEntity : TTEntityBase
{
    public string Name { get; set; }
}
