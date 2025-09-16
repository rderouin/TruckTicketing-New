using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.TruckTicketWellClassification.Base)]
public class TruckTicketWellClassificationUsageService : ServiceBase<TruckTicketWellClassificationUsageService, TruckTicketWellClassification, Guid>, IServiceBase<TruckTicketWellClassification, Guid>
{
    public TruckTicketWellClassificationUsageService(ILogger<TruckTicketWellClassificationUsageService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
