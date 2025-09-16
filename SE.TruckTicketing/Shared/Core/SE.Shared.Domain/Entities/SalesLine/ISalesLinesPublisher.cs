using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.Shared.Domain.Entities.SalesLine;

public interface ISalesLinesPublisher

{
    Task PublishSalesLines(IEnumerable<SalesLineEntity> salesLines, Operation operation = Operation.Update, bool includePreview = false);
}

public class SalesLinesPublisher : ISalesLinesPublisher
{
    private readonly IAppSettings _appSettings;

    private readonly IEntityPublisher _entityPublisher;

    private readonly ILog _log;

    public SalesLinesPublisher(IEntityPublisher entityPublisher, ILog log, IAppSettings appSettings)
    {
        _entityPublisher = entityPublisher;
        _log = log;
        _appSettings = appSettings;
    }

    public async Task PublishSalesLines(IEnumerable<SalesLineEntity> salesLines, Operation operation = Operation.Update, bool includePreview = false)
    {
        var statusToExclude = new List<SalesLineStatus>
        {
            SalesLineStatus.Exception,
            SalesLineStatus.Preview,
        };

        // exclude Preview & Exception lines by default; only keep if settings configured
        bool.TryParse(_appSettings.GetKeyOrDefault("PublishPreviewAndExceptionSalesLines", "false"), out var includePreviewAndExceptionLines);

        Func<SalesLineEntity, bool> filterSalesLinesToInclude = line => !includePreviewAndExceptionLines
                                                                            ? !statusToExclude.Contains(line.Status)
                                                                            : line.Status != SalesLineStatus.Preview || (line.Status == SalesLineStatus.Preview && includePreview);

        var entitiesToPublish = salesLines.Where(sl => sl.Status != default)
                                          .Where(filterSalesLinesToInclude)
                                          .ToList();

        // any left to publish?
        if (entitiesToPublish.Any())
        {
            // break them down by an invoice
            foreach (var invoiceGroup in entitiesToPublish.GroupBy(sl => sl.InvoiceId ?? sl.HistoricalInvoiceId))
            foreach (var group in invoiceGroup.GroupBy(GetOrderedGrouping).OrderBy(g => g.Key))
            {
                // publish a batch separately
                await _entityPublisher.EnqueueBulkMessage(group.Select(sl => sl).ToList(), operation.ToString(), (invoiceGroup.Key ?? Guid.NewGuid()).ToString());

                // warn about SLs that don't have the Invoice IDs
                if (!invoiceGroup.Key.HasValue)
                {
                    var listOfLines = string.Join(", ", group.Select(sl => sl.SalesLineNumber));
                    _log.Error(messageTemplate: $"Sales lines that do not have Invoice ID associated: {listOfLines}");
                }
            }
        }

        static int GetOrderedGrouping(SalesLineEntity sl)
        {
            return sl.Status switch
                   {
                       SalesLineStatus.Void => 1,
                       SalesLineStatus.Preview => 2,
                       _ => 3,
                   };
        }
    }
}
