using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services.SpartanProductParameter;

[Service(Service.SETruckTicketingApi, Routes.SpartanProductParameter_BaseRoute)]
public class SpartanProductParameterService : ServiceBase<SpartanProductParameterService, TruckTicketing.Contracts.Api.Models.SpartanProductParameters.SpartanProductParameter, Guid>, IServiceBase<TruckTicketing.Contracts.Api.Models.SpartanProductParameters.SpartanProductParameter, Guid>
{
    public SpartanProductParameterService(ILogger<SpartanProductParameterService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
