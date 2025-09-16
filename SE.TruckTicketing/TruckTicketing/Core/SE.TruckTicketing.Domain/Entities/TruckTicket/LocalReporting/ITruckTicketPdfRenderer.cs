using System;
using System.Collections.Generic;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public interface ITruckTicketPdfRenderer
{
    byte[] RenderTruckTicketStubs(ICollection<TruckTicketEntity> data);

    byte[] RenderMaterialApprovalScaleTicket(MaterialApprovalEntity materialApproval, FacilityEntity facility);

    byte[] RenderScaleTicket(TruckTicketEntity truckTicket, MaterialApprovalEntity materialApproval, FacilityEntity facilityEntity, string signature);

    byte[] RenderWorkTicket(TruckTicketEntity truckTicket);

    byte[] RenderFstDailyReport(IList<TruckTicketEntity> truckTickets, FSTWorkTicketRequest fstReportParameters);

    byte[] RenderLandfillDailyTicket(List<TruckTicketEntity> truckTickets, LandfillDailyReportRequest request);

    byte[] RenderLoadSummaryTicket(IEnumerable<TruckTicketEntity> truckTickets,
                                   Dictionary<Guid, MaterialApprovalEntity> materialApprovalEntities,
                                   LoadSummaryReportRequest request,
                                   FacilityEntity facility);

    byte[] RenderMaterialApprovalPdf(MaterialApprovalEntity materialApproval,
                                     Dictionary<Guid, AccountEntity> accounts,
                                     Dictionary<Guid, AccountContactEntity> accountsContacts,
                                     string signature,
                                     string facilityLocationCode,
                                     bool isFST = false);

    byte[] RenderProducerReport(List<TruckTicketEntity> truckTickets, ProducerReportRequest request);
}
