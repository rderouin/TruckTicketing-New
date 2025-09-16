using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public abstract class TicketJournalItem
{
    public enum TicketTypes
    {
        Undefined,

        WorkTicket,

        ScaleTicket,

        LandfillDailyTicket,
    }

    protected TicketJournalItem()
    {
    }

    protected TicketJournalItem(TruckTicketEntity truckTicket)
    {
        TicketNumber = truckTicket.TicketNumber;
        FacilityName = truckTicket.FacilityName;
        CountryCode = truckTicket.CountryCode;
    }

    protected TicketJournalItem(MaterialApprovalEntity materialApproval)
    {
        FacilityName = materialApproval.Facility;
        CountryCode = materialApproval.CountryCode;
    }

    public CountryCode CountryCode { get; set; }

    public abstract string DataSourceName { get; }

    public string Date { get; set; }

    public string FacilityName { get; set; }

    public string TicketNumber { get; set; }

    public abstract TicketTypes TicketType { get; }

    public string TicketNumberBarcode { get; set; }

    public string ReportName { get; set; } = string.Empty;
}
