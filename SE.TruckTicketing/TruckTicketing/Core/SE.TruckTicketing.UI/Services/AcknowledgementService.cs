using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Acknowledgement;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.acknowledgement)]
public class AcknowledgementService : ServiceBase<AcknowledgementService, Acknowledgement, Guid>, IAcknowledgementService
{
    public AcknowledgementService(ILogger<AcknowledgementService> logger,
                                  IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
