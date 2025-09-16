using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.legalEntity)]
public class LegalEntityService : ServiceBase<LegalEntityService, LegalEntity, Guid>, ILegalEntityService
{
    public LegalEntityService(ILogger<LegalEntityService> logger,
                              IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
