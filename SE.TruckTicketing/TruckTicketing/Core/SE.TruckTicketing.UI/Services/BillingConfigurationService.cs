using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.billingconfiguration)]
public class BillingConfigurationService : ServiceBase<BillingConfigurationService, BillingConfiguration, Guid>, IBillingConfigurationService
{
    public BillingConfigurationService(ILogger<BillingConfigurationService> logger,
                                       IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
