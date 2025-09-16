using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.facilities)]
public class FacilityService : ServiceBase<FacilityService, Facility, Guid>, IFacilityService
{
    public FacilityService(ILogger<FacilityService> logger,
                           IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
