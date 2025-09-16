using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.BillingService.Contracts.Api.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services.InvoiceExchange;

[Service(Service.SEBillingServiceApi, Service.Resources.InvoiceExchangeSourceFields)]
public class SourceFieldService : ServiceBase<SourceFieldService, SourceFieldDto, Guid>, IServiceBase<SourceFieldDto, Guid>
{
    public SourceFieldService(ILogger<SourceFieldService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
