using System;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.VolumeChange;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.VolumeChange, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.VolumeChange)]
[GenerateManager]
[GenerateProvider]
public class VolumeChangeEntity : TTAuditableEntityBase, IFacilityRelatedEntity
{
    public DateTimeOffset TicketDate { get; set; }

    public string TicketNumber { get; set; }

    public Stream ProcessOriginal { get; set; }

    public double OilVolumeOriginal { get; set; }

    public double WaterVolumeOriginal { get; set; }

    public double SolidVolumeOriginal { get; set; }

    public double TotalVolumeOriginal { get; set; }

    public Stream ProcessAdjusted { get; set; }

    public double OilVolumeAdjusted { get; set; }

    public double WaterVolumeAdjusted { get; set; }

    public double SolidVolumeAdjusted { get; set; }

    public double TotalVolumeAdjusted { get; set; }

    public VolumeChangeReason VolumeChangeReason { get; set; }

    public string VolumeChangeReasonText { get; set; }

    public string FacilityName { get; set; }

    public TruckTicketStatus TruckTicketStatus { get; set; }

    public Guid FacilityId { get; set; }
}
