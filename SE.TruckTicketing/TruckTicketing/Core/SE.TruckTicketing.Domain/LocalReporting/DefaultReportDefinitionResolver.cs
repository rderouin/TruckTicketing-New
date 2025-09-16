using System.IO;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Caching;

namespace SE.TruckTicketing.Domain.LocalReporting;

public class DefaultReportDefinitionResolver : IReportDefinitionResolver
{
    private readonly ICachingManager _cachingManager;

    public DefaultReportDefinitionResolver(ICachingManager cachingManager)
    {
        _cachingManager = cachingManager;
    }

    public System.IO.Stream GetReportDefinition<TReportItem>(TReportItem item) where TReportItem : TicketJournalItem
    {
        var reportItem = typeof(TReportItem).Name;

        var reportDefinition = _cachingManager.Get<byte[]>(reportItem);

        if (reportDefinition == default)
        {
            var countryCode = item.CountryCode == CountryCode.Undefined ? "" : item.CountryCode.ToString();
            var ticketType = item.TicketType == TicketJournalItem.TicketTypes.Undefined ? "" : item.TicketType.ToString();
            var reportDefinitionFilePath = $"ReportDefinitions/{ticketType}{item.ReportName}{countryCode}.rdl";
            reportDefinition = File.ReadAllBytes(reportDefinitionFilePath);
            _cachingManager.Set(reportItem, reportDefinition);
        }

        return new MemoryStream(reportDefinition);
    }
}
