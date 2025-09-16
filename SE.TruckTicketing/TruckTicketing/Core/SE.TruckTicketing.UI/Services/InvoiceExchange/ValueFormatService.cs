using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.BillingService.Contracts.Api.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services.InvoiceExchange;

[Service(Service.SEBillingServiceApi, Service.Resources.InvoiceExchangeValueFormats)]
public class ValueFormatService : ServiceBase<ValueFormatService, ValueFormatDto, Guid>, IServiceBase<ValueFormatDto, Guid>
{
    public ValueFormatService(ILogger<ValueFormatService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
