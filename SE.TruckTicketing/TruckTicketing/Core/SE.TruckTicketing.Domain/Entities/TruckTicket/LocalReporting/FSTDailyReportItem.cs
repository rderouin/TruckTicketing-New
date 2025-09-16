using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class FstDailyReportItem : TicketJournalItem
{
    public FstDailyReportItem(TruckTicketEntity truckTicket) : base(truckTicket)
    {
        ReportName = "FSTDaily";
    }

    public string ShipDate { get; set; }

    public string Total { get; set; }

    public string Oil { get; set; }

    public string OilPercent { get; set; }

    public string Water { get; set; }

    public string WaterPercent { get; set; }

    public string Solids { get; set; }

    public string SolidsPercent { get; set; }

    public string OilDensity { get; set; }

    public string WellClass { get; set; }

    public string Substance { get; set; }

    public string WasteCode { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public string Tenorm { get; set; }

    public string MaterialApprovalDescription { get; set; }

    public string TruckingCompany { get; set; }

    public string BillOfLading { get; set; }

    public string ManifestNumber { get; set; }

    public string DOW { get; set; }

    public string Destination { get; set; }

    public string AdditionalServicesQty { get; set; }

    public string LCFrequency { get; set; }

    public string ServiceType { get; set; }

    public string CustomerName { get; set; }

    public string BillingCustomer { get; set; }

    public string SourceLocation { get; set; }

    public override string DataSourceName => nameof(FstDailyReportItem);

    public override TicketTypes TicketType => TicketTypes.WorkTicket;

    public string GeneratorName { get; set; }

    public string BillingConfigName { get; set; }
}
