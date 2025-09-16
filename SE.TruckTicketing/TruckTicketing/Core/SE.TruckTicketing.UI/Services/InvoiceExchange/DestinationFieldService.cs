using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.BillingService.Contracts.Api.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services.InvoiceExchange;

[Service(Service.SEBillingServiceApi, Service.Resources.InvoiceExchangeDestinationFields)]
public class DestinationFieldService : ServiceBase<DestinationFieldService, DestinationFieldDto, Guid>, IServiceBase<DestinationFieldDto, Guid>
{
    public DestinationFieldService(ILogger<DestinationFieldService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
