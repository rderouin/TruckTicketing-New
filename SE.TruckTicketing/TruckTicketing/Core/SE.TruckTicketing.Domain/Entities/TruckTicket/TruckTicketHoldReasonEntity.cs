using SE.Shared.Domain;
using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(TruckTicketHoldReasonEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.TruckTicketHoldReason)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class TruckTicketHoldReasonEntity : TTAuditableEntityBase
{
    public string HoldReason { get; set; }

    public bool IsDeleted { get; set; }
}
