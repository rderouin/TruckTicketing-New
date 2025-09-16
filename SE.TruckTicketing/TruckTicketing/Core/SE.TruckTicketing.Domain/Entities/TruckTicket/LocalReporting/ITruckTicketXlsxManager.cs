using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public interface ITruckTicketXlsxManager : IManager
{
    Task<byte[]> CreateLandfillDailyReport(LandfillDailyReportRequest landfillRequest);

    Task<byte[]> CreateFstDailyReport(FSTWorkTicketRequest fstReportRequest);
}
