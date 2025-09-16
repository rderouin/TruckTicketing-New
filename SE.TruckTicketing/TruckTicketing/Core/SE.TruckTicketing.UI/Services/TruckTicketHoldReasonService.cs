using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.TruckTicketHoldReason_BaseRoute)]
public class TruckTicketHoldReasonService : ServiceBase<TruckTicketHoldReasonService, TruckTicketHoldReason, Guid>, IServiceBase<TruckTicketHoldReason, Guid>
{
    public TruckTicketHoldReasonService(ILogger<TruckTicketHoldReasonService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
