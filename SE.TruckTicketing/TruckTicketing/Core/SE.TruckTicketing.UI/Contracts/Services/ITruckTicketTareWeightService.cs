using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ITruckTicketTareWeightService : IServiceBase<TruckTicketTareWeight, Guid>
{
    Task<List<TruckTicketTareWeightCsvResult>> UploadTruckTicketTareWeight(IEnumerable<TruckTicketTareWeightCsv> records);
}
