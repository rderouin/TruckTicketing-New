using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class ScaleTicketJournalItem : TicketJournalItem
{
    public ScaleTicketJournalItem() { }

    public ScaleTicketJournalItem(TruckTicketEntity truckTicket) : base(truckTicket)
    {
    }

    public string BillOfLadingNumber { get; set; }

    public string Class { get; set; }

    public override string DataSourceName => nameof(ScaleTicketJournalItem);

    public string GeneratorName { get; set; }

    public string GrossWeight { get; set; }

    public string ManifestNumber { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public string NetWeight { get; set; }

    public string ProductCharacterization { get; set; }

    public string RigNumber { get; set; }

    public string SecureEmail { get; set; }

    public string SecureFacilityLSD { get; set; }

    public string SecureFacilityName { get; set; }

    public string SecurePhone { get; set; }

    public string Signatory1Email { get; set; }

    public string Signatory1Name { get; set; }

    public string Signatory1Phone { get; set; }

    public string Signatory2Email { get; set; }

    public string Signatory2Name { get; set; }

    public string Signatory2Phone { get; set; }

    public string SourceLocationProductReceivedFrom { get; set; }

    public string TareWeight { get; set; }

    public string TimeIn { get; set; }

    public string TimeOut { get; set; }

    public string TnormsLevel { get; set; }

    public string TruckingCompanyName { get; set; }

    public string TruckUnitNumber { get; set; }

    public string SecureRepSignature { get; set; }

    public override TicketTypes TicketType => TicketTypes.ScaleTicket;
}
