using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.BillingService.Contracts.Api.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services.InvoiceExchange;

[Service(Service.SEBillingServiceApi, Service.Resources.InvoiceExchange)]
public class InvoiceExchangeService : ServiceBase<InvoiceExchangeService, InvoiceExchangeDto, Guid>, IServiceBase<InvoiceExchangeDto, Guid>
{
    public InvoiceExchangeService(ILogger<InvoiceExchangeService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
