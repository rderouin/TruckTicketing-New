using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.FacilityService.Base)]
public class FacilityServiceService : ServiceBase<FacilityServiceService, TruckTicketing.Contracts.Models.FacilityServices.FacilityService, Guid>, IFacilityServiceService
{
    public FacilityServiceService(ILogger<FacilityServiceService> logger,
                                  IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}

[Service(Service.SETruckTicketingApi, Routes.FacilityServiceSubstanceIndex.Base)]
public class FacilityServiceSubstanceIndexService : ServiceBase<FacilityServiceSubstanceIndexService, FacilityServiceSubstanceIndex, Guid>, IFacilityServiceSubstanceIndexService
{
    public FacilityServiceSubstanceIndexService(ILogger<FacilityServiceSubstanceIndexService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
