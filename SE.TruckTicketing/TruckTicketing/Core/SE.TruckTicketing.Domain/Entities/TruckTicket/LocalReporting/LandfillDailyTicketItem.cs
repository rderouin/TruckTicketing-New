using System.Collections.Generic;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class LandfillDailyTicketItem : TicketJournalItem
{
    public LandfillDailyTicketItem() { }

    public LandfillDailyTicketItem(TruckTicketEntity truckTicket) : base(truckTicket)
    {
    }

    public string ShipDate { get; set; }

    public string CellCoord { get; set; }

    public string ScaleTicket { get; set; }

    public string NetWeight { get; set; }

    public string Unit { get; set; }

    public string AltQty { get; set; }

    public string AltUnit { get; set; }

    public string SubstanceName { get; set; }

    public string WasteCode { get; set; }

    public string BillOfLading { get; set; }

    public string TruckingCompany { get; set; }

    public string TruckUnitNumber { get; set; }

    public string TimeIn { get; set; }

    public string TimeOut { get; set; }

    public string ManifestNumber { get; set; }

    public string Tenorm { get; set; }

    public string AdditionalServiceQuantity { get; set; }

    //Grouping Data

    public string Class { get; set; }

    public string GeneratorName { get; set; }

    public string BillingCustomer { get; set; }

    public string SourceLocation { get; set; }

    public string LegalEntity { get; set; }

    public string UserName { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public string BillingConfigName { get; set; }

    public override string DataSourceName => nameof(LandfillDailyTicketItem);

    public override TicketTypes TicketType => TicketTypes.LandfillDailyTicket;
}

public class LandfillDailyReportInputParameters
{
    public FacilityEntity Facility { get; set; }

    public IEnumerable<MaterialApprovalEntity> MaterialApprovals { get; set; }

    public IEnumerable<TruckTicketEntity> LandfillTruckTickets { get; set; }
}
