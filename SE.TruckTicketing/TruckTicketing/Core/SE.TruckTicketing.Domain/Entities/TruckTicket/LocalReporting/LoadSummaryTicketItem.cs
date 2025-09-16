using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class LoadSummaryTicketItem : TicketJournalItem
{
    public LoadSummaryTicketItem() { }

    public LoadSummaryTicketItem(TruckTicketEntity truckTicket) : base(truckTicket)
    {
        ReportName = "LoadSummary";
    }

    public string SourceLocation { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public string GeneratorName { get; set; }

    public string BillingCustomer { get; set; }

    public string Substance { get; set; }

    public string WasteCode { get; set; }

    public string AFE { get; set; }

    public string PO { get; set; }

    public string SignatoryName { get; set; }

    public string TransactionDate { get; set; }

    public string TruckingCompanyName { get; set; }

    public string BillOfLading { get; set; }

    public string Facility { get; set; }

    public string Unit { get; set; }

    public string TimeIn { get; set; }

    public string TimeOut { get; set; }

    public string TrackingNumber { get; set; }

    public string ManifestNumber { get; set; }

    public double GrossWeight { get; set; }

    public double TareWeight { get; set; }

    public double NetWeight { get; set; }

    public override string DataSourceName => nameof(LoadSummaryTicketItem);

    public override TicketTypes TicketType => TicketTypes.ScaleTicket;
}
