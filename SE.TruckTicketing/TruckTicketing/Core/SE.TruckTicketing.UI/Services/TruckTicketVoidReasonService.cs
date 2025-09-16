using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.TruckTicketVoidReason_BaseRoute)]
public class TruckTicketVoidReasonService : ServiceBase<TruckTicketVoidReasonService, TruckTicketVoidReason, Guid>, IServiceBase<TruckTicketVoidReason, Guid>
{
    public TruckTicketVoidReasonService(ILogger<TruckTicketVoidReasonService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
