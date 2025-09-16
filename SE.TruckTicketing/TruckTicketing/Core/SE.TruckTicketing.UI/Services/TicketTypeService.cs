using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.tickettypes)]
public class TicketTypeService : ServiceBase<TicketTypeService, TicketType, Guid>, ITicketTypeService
{
    public TicketTypeService(ILogger<TicketTypeService> logger,
                             IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
