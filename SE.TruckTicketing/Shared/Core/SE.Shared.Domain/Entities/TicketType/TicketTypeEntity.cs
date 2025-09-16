using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.TicketType;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(TicketTypeEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.TicketType)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class TicketTypeEntity : TTEntityBase
{
    public string TicketTypeName { get; set; }

    public string TicketTypeCode { get; set; }
}
