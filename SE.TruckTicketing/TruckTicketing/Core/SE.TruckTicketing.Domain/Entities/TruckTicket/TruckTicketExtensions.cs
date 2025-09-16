using System;

using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;
public static class TruckTicketExtensions
{
    public static bool HasTimeOut(this TruckTicketEntity ticket)
    {
        return ticket.TimeOut != null;
    }

    public static bool HasNetWeight(this TruckTicketEntity ticket)
    {
        return ticket.NetWeight > 0;
    }

    public static bool IsNotVoid(this TruckTicketEntity ticket)
    {
        return ticket.Status != TruckTicketStatus.Void;
    }

    public static bool EffectiveDateIsGreaterThanOrEqualTo(this TruckTicketEntity ticket, DateTime startDate)
    {
        return ticket.EffectiveDate >= startDate;
    }

    public static bool EffectiveDateIsLessThanOrEqualTo(this TruckTicketEntity ticket, DateTime endDate)
    {
        return ticket.EffectiveDate <= endDate;
    }

    public static bool MeetsReportConditions(this TruckTicketEntity ticket, DateTime startDate, DateTime endDate)
    {
        return ticket.EffectiveDateIsGreaterThanOrEqualTo(startDate) &&
               ticket.EffectiveDateIsLessThanOrEqualTo(endDate) &&
               ticket.HasTimeOut() && //Bug 11670
               ticket.HasNetWeight() && //Bug 11670
               ticket.IsNotVoid();
    }
}
