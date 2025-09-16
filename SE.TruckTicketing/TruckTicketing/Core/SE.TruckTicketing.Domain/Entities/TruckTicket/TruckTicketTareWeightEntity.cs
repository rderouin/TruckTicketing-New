using System;

using SE.Shared.Domain;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.TruckTicketTareWeight, PartitionKeyType.WellKnown)]
[Discriminator(Databases.Discriminators.TruckTicketTareWeightIndex, Property = nameof(EntityType))]
[GenerateProvider]
public class TruckTicketTareWeightEntity : TTAuditableEntityBase
{
    public string TruckingCompanyName { get; set; }

    public Guid? TruckingCompanyId { get; set; }

    public string TruckNumber { get; set; }

    public string TrailerNumber { get; set; }

    public DateTimeOffset LoadDate { get; set; }

    public Guid? FacilityId { get; set; }

    public string FacilityName { get; set; }

    public string FacilitySiteId { get; set; }

    public Guid TicketId { get; set; }

    public string TicketNumber { get; set; }

    public double TareWeight { get; set; }

    public bool IsActivated { get; set; } = true;
}
