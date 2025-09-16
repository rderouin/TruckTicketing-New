using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class WorkTicketJournalItem : TicketJournalItem
{
    public WorkTicketJournalItem()
    {
    }

    public WorkTicketJournalItem(TruckTicketEntity truckTicket) : base(truckTicket)
    {
    }

    public override string DataSourceName => nameof(WorkTicketJournalItem);

    public override TicketTypes TicketType => TicketTypes.WorkTicket;
}
