using SE.Shared.Domain;
using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(TruckTicketVoidReasonEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.TruckTicketVoidReason)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class TruckTicketVoidReasonEntity : TTAuditableEntityBase
{
    public string VoidReason { get; set; }

    public bool IsDeleted { get; set; }
}
