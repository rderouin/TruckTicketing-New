using System;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.TruckTicketWellClassification, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.TruckTicketWellClassification)]
[GenerateManager]
[GenerateProvider]
public class TruckTicketWellClassificationUsageEntity : TTEntityBase
{
    public Guid TruckTicketId { get; set; }

    public string TicketNumber { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationIdentifier { get; set; }

    public Guid FacilityId { get; set; }

    public string FacilityName { get; set; }

    public DateTimeOffset Date { get; set; }

    public WellClassifications WellClassification { get; set; }
}
