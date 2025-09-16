using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class ProducerReportItem : TicketJournalItem
{
    public ProducerReportItem(TruckTicketEntity truckTicket) : base(truckTicket)
    {
        ReportName = "ProducerReport";
    }

    public string SourceLocation { get; set; }

    public string BatteryCode { get; set; }

    public string OperatorName { get; set; }

    public string ServiceTypeId { get; set; }

    public string FacilityCodeType { get; set; }

    public string ShipDate { get; set; }

    public string Total { get; set; }

    public string Oil { get; set; }

    public string OilPercentage { get; set; }

    public string Water { get; set; }

    public string WaterPercentage { get; set; }

    public string Solid { get; set; }

    public string SolidPercentage { get; set; }

    public string NetPrice { get; set; }

    public string WasteCode { get; set; }

    public string TruckCompany { get; set; }

    public string TruckNumber { get; set; }

    public string BillOfLading { get; set; }

    public string TimeIn { get; set; }

    public string TimeOut { get; set; }

    public string OilDensity { get; set; }

    public string TruckUnit { get; set; }

    public string ServiceTypeName { get; set; }

    public string StreamCode { get; set; }

    public override string DataSourceName => nameof(ProducerReportItem);

    public override TicketTypes TicketType => TicketTypes.Undefined;
}

public class ProducerReportParameters
{
    public string FromDate { get; set; }

    public string ToDate { get; set; }

    public string Facilities { get; set; }

    public string LegalEntity { get; set; }

    public bool PriceOnLoad { get; set; }

    public string Generators { get; set; }

    public string FacilityLocationCode { get; set; }
}
