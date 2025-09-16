using System;

using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;

namespace SE.TruckTicketing.Contracts.Api.Models.SpartanData;

public class SpartanSummaryModel
{
    public string WaybillNumber { get; set; }

    public double CorrectedOilDensity { get; set; }

    public double CorrectedShrinkageEmulsionVolume { get; set; }

    public double RoundedCorrectedOilVolume { get; set; }

    public double RoundedCorrectedWaterVolume { get; set; }

    public double RoundedCorrectedEmulsionWaterCut { get; set; }

    public double RoundedCorrectedEmulsionOilCut { get; set; }

    public double RoundedCorrectedEmulsionVolume { get; set; }

    public string FacilityOperatorName { get; set; }

    public string LocationUwi { get; set; }

    public string ProductName { get; set; }

    public string TicketId { get; set; }

    public string TransactionId { get; set; }

    public string PlantIdentifier { get; set; }

    public DateTimeOffset TransferStartTime { get; set; }

    public DateTimeOffset TransferFinishTime { get; set; }

    public string TransportCompanyName { get; set; }

    public string TransportVehicleUnitNumber { get; set; }

    public LocationOperatingStatus LocationOperatingStatus { get; set; }

    public string CustomerTransactionId { get; set; }

    public string ScheduleNumber { get; set; }
}
