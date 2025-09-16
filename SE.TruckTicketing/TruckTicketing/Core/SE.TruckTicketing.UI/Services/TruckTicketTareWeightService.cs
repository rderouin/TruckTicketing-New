using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.TruckTicketTareWeight.Base)]
public class TruckTicketTareWeightService : ServiceBase<TruckTicketTareWeightService, TruckTicketTareWeight, Guid>, ITruckTicketTareWeightService
{
    public TruckTicketTareWeightService(ILogger<TruckTicketTareWeightService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    public async Task<List<TruckTicketTareWeightCsvResult>> UploadTruckTicketTareWeight(IEnumerable<TruckTicketTareWeightCsv> records)
    {
        var response = await SendRequest<List<TruckTicketTareWeightCsvResult>>(nameof(HttpMethod.Post), Routes.TruckTicketTareWeight.Base, records);

        return response?.Model ?? new List<TruckTicketTareWeightCsvResult>();
    }
}
