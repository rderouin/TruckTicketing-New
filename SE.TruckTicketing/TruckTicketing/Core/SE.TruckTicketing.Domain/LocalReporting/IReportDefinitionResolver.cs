using System.IO;

using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

namespace SE.TruckTicketing.Domain.LocalReporting;

public interface IReportDefinitionResolver
{
    Stream GetReportDefinition<TReportItem>(TReportItem item) where TReportItem : TicketJournalItem;
}
