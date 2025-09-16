using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.serviceType)]
public class ServiceTypeService : ServiceBase<ServiceTypeService, ServiceType, Guid>, IServiceTypeService
{
    public ServiceTypeService(ILogger<ServiceTypeService> logger,
                              IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
