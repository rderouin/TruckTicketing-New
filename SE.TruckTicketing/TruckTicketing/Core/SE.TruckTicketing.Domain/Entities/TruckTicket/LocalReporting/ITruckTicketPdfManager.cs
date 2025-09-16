using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Contracts.Api;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public interface ITruckTicketPdfManager : IManager
{
    Task<byte[]> CreateLandfillDailyReport(LandfillDailyReportRequest landfillRequest);

    Task<byte[]> CreateTicketPrint(CompositeKey<Guid> truckTicketKey);

    Task<byte[]> CreateFstDailyReport(FSTWorkTicketRequest fstReportRequest);

    Task<byte[]> CreateLoadSummaryReport(LoadSummaryReportRequest loadSummaryRequest);

    Task<byte[]> CreateProducerReport(ProducerReportRequest producerReportRequest);
}
