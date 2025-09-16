using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Substances;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.substances)]
public class SubstanceService : ServiceBase<SubstanceService, Substance, Guid>, ISubstanceService
{
    public SubstanceService(ILogger<SubstanceService> logger,
                            IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
