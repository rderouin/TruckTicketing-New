using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.additionalservicesconfiguration)]
public class AdditionalServicesConfigurationService : ServiceBase<AdditionalServicesConfigurationService, AdditionalServicesConfiguration, Guid>, IAdditionalServicesConfigurationService
{
    public AdditionalServicesConfigurationService(ILogger<AdditionalServicesConfigurationService> logger, IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
