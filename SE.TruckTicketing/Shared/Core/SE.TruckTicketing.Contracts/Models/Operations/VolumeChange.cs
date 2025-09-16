using System;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class VolumeChange : GuidApiModelBase
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
    
    public Guid FacilityId { get; set; }

    public string FacilityName { get; set; }

    public TruckTicketStatus TruckTicketStatus { get; set; }
}
