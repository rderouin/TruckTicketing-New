using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Domain.Entities.SalesLine;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationHelper
{
    public static void SetLoadConfirmationTicketStartEndDates(LoadConfirmationEntity lc, List<SalesLineEntity> salesLines)
    {
        // fallback values - edge-case scenarios
        lc.TicketStartDate = DateTime.Today;
        lc.TicketEndDate = DateTime.Today;

        if (!salesLines.Any())
        {
            return;
        }

        // prefilter for both
        var filteredSalesLines = salesLines.Where(sl => sl.Status != SalesLineStatus.Void && sl.TruckTicketEffectiveDate.HasValue).ToList();

        if (!filteredSalesLines.Any())
        {
            return;
        }

        // update dates
        lc.TicketStartDate = filteredSalesLines.MinBy(sl => sl.TruckTicketEffectiveDate).TruckTicketEffectiveDate!.Value;
        lc.TicketEndDate = salesLines.MaxBy(sl => sl.TruckTicketEffectiveDate).TruckTicketEffectiveDate!.Value;
    }
}
