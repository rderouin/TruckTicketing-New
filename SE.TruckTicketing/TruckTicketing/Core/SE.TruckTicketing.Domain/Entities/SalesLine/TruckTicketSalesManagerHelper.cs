using System.Collections.Generic;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

/*classes like these help to improve readability elsewhere, i feel. We can respectfully debate if you wish.*/
internal class TruckTicketSalesManagerHelper
{
    public static void SetHazardousNonHazardous(MaterialApprovalEntity materialApproval, TruckTicketEntity truckTicket)
    {
        if (materialApproval != null && truckTicket.FacilityType == FacilityType.Lf)
        {
            if (materialApproval.HazardousNonhazardous == HazardousClassification.Hazardous)
            {
                truckTicket.DowNonDow = DowNonDow.Hazardous;
            }
            else
            {
                truckTicket.DowNonDow = DowNonDow.NonHazardous;//undefined is not an option for LFs.
            }
        }
    }

    public static bool TicketWasNotJustApproved(TruckTicketEntity existingTicket, TruckTicketEntity newTicket)
    {
        return !WasTicketJustApproved(existingTicket, newTicket);
    }

    public static bool WasTicketJustApproved(TruckTicketEntity existingTicket, TruckTicketEntity newTicket)
    {
        return existingTicket?.Status is TruckTicketStatus.Open or TruckTicketStatus.Hold && newTicket.Status is TruckTicketStatus.Approved;
    }

    /* I moved this chunk of code here for testability. Code smell alert: Status = Serious. */
    public static void UpdateTruckTicketFromSalesLines(TruckTicketEntity truckTicket, List<SalesLineEntity> salesLines)
    {
        var additionalServicesQuantity = 0;

        foreach (var salesLine in salesLines)
        {
            salesLine.ApplyFoRounding();

            if (truckTicket.SalesLineIds != null)
            {
                truckTicket.SalesLineIds.List.Add(salesLine.Id.ToString());
            }

            if (salesLine.IsAdditionalService)
            {
                salesLine.TruckTicketEffectiveDate = truckTicket.EffectiveDate;
                additionalServicesQuantity++;
            }
        }

        truckTicket.AdditionalServicesQty = additionalServicesQuantity;
        
    }

    public static bool ShouldUpdateSalesLines(TruckTicketEntity truckTicket, List<SalesLineEntity> salesLines)
    {
        return !(truckTicket.Status is TruckTicketStatus.Void || salesLines.Count == 0 || !truckTicket.TicketNumber.HasText());
    }

    public static void TryTransitioningTicketToOpenStatus(TruckTicketEntity truckTicket)
    {
        if (truckTicket.Status is TruckTicketStatus.Stub or TruckTicketStatus.New)
        {
            truckTicket.Status = TruckTicketStatus.Open;
        }
    }
}
