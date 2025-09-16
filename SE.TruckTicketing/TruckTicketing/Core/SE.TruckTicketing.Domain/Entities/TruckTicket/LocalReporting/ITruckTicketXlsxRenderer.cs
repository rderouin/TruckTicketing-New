using System;
using System.Collections.Generic;

using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public interface ITruckTicketXlsxRenderer
{
    byte[] RenderFstDailyReport(IList<TruckTicketEntity> truckTickets, FSTWorkTicketRequest fstReportParameters);

    byte[] RenderLandfillDailyTicket(List<TruckTicketEntity> truckTickets, LandfillDailyReportRequest request);

}
