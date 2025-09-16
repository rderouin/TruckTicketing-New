using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Trident.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public interface ITruckTicketTareWeightManager : IManager<Guid, TruckTicketTareWeightEntity>
{
    Task<List<TruckTicketTareWeightCsvResponse>> TruckTicketTareWeightCsvProcessing(IEnumerable<TruckTicketTareWeightCsvResponse> request);
}
