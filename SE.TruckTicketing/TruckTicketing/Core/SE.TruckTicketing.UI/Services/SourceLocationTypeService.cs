using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.SourceLocationType_BaseRoute)]
public class SourceLocationTypeService : ServiceBase<SourceLocationTypeService, SourceLocationType, Guid>, IServiceBase<SourceLocationType, Guid>
{
    public SourceLocationTypeService(ILogger<SourceLocationTypeService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
